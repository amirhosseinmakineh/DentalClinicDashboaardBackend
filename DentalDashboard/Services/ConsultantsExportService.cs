using DentalDashboard.Domain.Models;
using DentalDashboard.Infrastracture.Context;
using DentalDashboard.Utilities.Convertor;
using DentalDashboard.Utilities.Time;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Services;

public class ConsultantsExportService
{
    private readonly DentalContext context;

    public ConsultantsExportService(DentalContext context) => this.context = context;

    public async Task<byte[]> ExportCsvAsync(CancellationToken cancellationToken = default)
    {
        var consultants = await context.ConsultantProfiles.AsNoTracking()
            .Include(x => x.User).ThenInclude(x => x.UserRoles).ThenInclude(x => x.Role)
            .Where(x => !x.IsDeleted &&
                        x.User != null &&
                        x.User.UserRoles.Any(ur => ur.Role != null && ur.Role.RoleName == "Consultant"))
            .OrderBy(x => x.User!.LastName).ThenBy(x => x.User!.FirstName)
            .ToListAsync(cancellationToken);

        var consultantIds = consultants.Select(x => x.Id).ToList();

        var leadAssignments = await context.LeadAssignments.AsNoTracking()
            .Where(x => !x.IsDeleted && x.ConsultantProfileId.HasValue && consultantIds.Contains(x.ConsultantProfileId.Value))
            .OrderBy(x => x.ConsultantProfileId).ThenByDescending(x => x.AssignedAt)
            .ToListAsync(cancellationToken);

        var leadsByConsultant = leadAssignments
            .GroupBy(x => x.ConsultantProfileId!.Value)
            .ToDictionary(x => x.Key, x => x.ToList());

        var reservationStats = await context.Reservations.AsNoTracking()
            .Where(x => !x.IsDeleted && consultantIds.Contains(x.ConsultantProfileId))
            .GroupBy(x => x.ConsultantProfileId)
            .Select(g => new
            {
                ConsultantProfileId = g.Key,
                TotalReservations = g.Count(),
                ActiveReservations = g.Count(x => !x.IsCanceled),
                ConsultantConfirmed = g.Count(x => x.ConsultantAttendanceConfirmedAt != null)
            })
            .ToDictionaryAsync(x => x.ConsultantProfileId, cancellationToken);

        var (todayStartUtc, todayEndUtc) = IranTimeHelper.GetIranDayRangeAsUtc(IranTimeHelper.TodayInIran());
        var todayReservationCounts = await context.Reservations.AsNoTracking()
            .Where(x => !x.IsDeleted &&
                        !x.IsCanceled &&
                        consultantIds.Contains(x.ConsultantProfileId) &&
                        x.CreatedAt >= todayStartUtc &&
                        x.CreatedAt < todayEndUtc)
            .GroupBy(x => x.ConsultantProfileId)
            .Select(g => new { ConsultantProfileId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ConsultantProfileId, x => x.Count, cancellationToken);

        var lines = new List<string>
        {
            CsvExportHelper.JoinRow("بخش خلاصه مشاوران"),
            CsvExportHelper.JoinRow(
                "شناسه مشاور",
                "نام",
                "نام خانوادگی",
                "موبایل",
                "کد ملی",
                "وضعیت آنلاین",
                "وضعیت حضور",
                "آخرین بازدید",
                "آخرین آنلاین",
                "آخرین آفلاین",
                "تعداد کل لیدها",
                "تعداد رزرو",
                "تعداد رزرو فعال",
                "رزروهای امروز",
                "تعداد تایید حضور مشاور",
                "تعداد تماس گرفته",
                "تعداد تماس نگرفته",
                "تعداد تبدیل شده",
                "تعداد رد شده",
                "تعداد منقضی شده")
        };

        foreach (var consultant in consultants)
        {
            var leads = leadsByConsultant.GetValueOrDefault(consultant.Id) ?? [];
            var calledCount = leads.Count(IsLeadCalled);
            var notCalledCount = leads.Count(x => !IsLeadCalled(x));
            var convertedCount = leads.Count(x => x.LeadAssignmentState == LeadAssignmentState.Converted);
            var rejectedCount = leads.Count(x => x.LeadAssignmentState == LeadAssignmentState.Rejected);
            var expiredCount = leads.Count(x => x.LeadAssignmentState == LeadAssignmentState.Expired);
            var stats = reservationStats.GetValueOrDefault(consultant.Id);

            lines.Add(CsvExportHelper.JoinRow(
                consultant.Id.ToString(),
                consultant.User?.FirstName ?? string.Empty,
                consultant.User?.LastName ?? string.Empty,
                consultant.User?.PhoneNumber ?? string.Empty,
                consultant.NationalCode,
                AdminReportPersianLabels.ToYesNo(consultant.IsOnline),
                AdminReportPersianLabels.ToYesNo(consultant.IsAvailable),
                consultant.User?.LastSeenAt.HasValue == true
                    ? DateConvertor.ToPersianDateTimeString(consultant.User.LastSeenAt.Value)
                    : string.Empty,
                consultant.LastOnlineAt.HasValue
                    ? DateConvertor.ToPersianDateTimeString(consultant.LastOnlineAt.Value)
                    : string.Empty,
                consultant.LastOfflineAt.HasValue
                    ? DateConvertor.ToPersianDateTimeString(consultant.LastOfflineAt.Value)
                    : string.Empty,
                leads.Count.ToString(),
                stats?.TotalReservations.ToString() ?? "0",
                stats?.ActiveReservations.ToString() ?? "0",
                todayReservationCounts.GetValueOrDefault(consultant.Id).ToString(),
                stats?.ConsultantConfirmed.ToString() ?? "0",
                calledCount.ToString(),
                notCalledCount.ToString(),
                convertedCount.ToString(),
                rejectedCount.ToString(),
                expiredCount.ToString()));
        }

        lines.Add(string.Empty);
        lines.Add(CsvExportHelper.JoinRow("بخش جزئیات تماس لیدها"));
        lines.Add(CsvExportHelper.JoinRow(
            "شناسه مشاور",
            "نام مشاور",
            "موبایل مشاور",
            "شناسه لید",
            "نام لید",
            "موبایل لید",
            "تاریخ ایجاد لید",
            "وضعیت لید",
            "نوع تخصیص",
            "تاریخ تخصیص",
            "وضعیت تماس",
            "نتیجه تماس",
            "تاریخ تماس",
            "تاریخ ثبت گزارش",
            "متن گزارش",
            "شهر بیمار",
            "منطقه بیمار",
            "نام بیزینس",
            "احتمال حضور (درصد)",
            "شماره دوم بیمار"));

        foreach (var consultant in consultants)
        {
            if (!leadsByConsultant.TryGetValue(consultant.Id, out var leads))
                continue;

            var consultantFullName = $"{consultant.User?.FirstName ?? string.Empty} {consultant.User?.LastName ?? string.Empty}".Trim();

            foreach (var lead in leads)
            {
                var hasCalled = IsLeadCalled(lead);

                lines.Add(CsvExportHelper.JoinRow(
                    consultant.Id.ToString(),
                    consultantFullName,
                    consultant.User?.PhoneNumber ?? string.Empty,
                    lead.Id.ToString(),
                    lead.UserName,
                    lead.PhoneNumber,
                    DateConvertor.ToPersianDateTimeString(
                        IranTimeHelper.ToIranLocalTime(lead.CreatedAt)),
                    lead.LeadAssignmentState.ToPersian(),
                    lead.AssignmentType.ToPersian(),
                    lead.AssignedAt.HasValue
                        ? DateConvertor.ToPersianDateTimeString(
                            IranTimeHelper.ToIranLocalTime(lead.AssignedAt.Value))
                        : string.Empty,
                    AdminReportPersianLabels.ToCallStatus(hasCalled),
                    lead.CallResult.HasValue ? lead.CallResult.Value.ToPersian() : string.Empty,
                    lead.ContactedAt.HasValue
                        ? DateConvertor.ToPersianDateTimeString(
                            IranTimeHelper.ToIranLocalTime(lead.ContactedAt.Value))
                        : string.Empty,
                    lead.ReportSubmittedAt.HasValue
                        ? DateConvertor.ToPersianDateTimeString(
                            IranTimeHelper.ToIranLocalTime(lead.ReportSubmittedAt.Value))
                        : string.Empty,
                    lead.ReportDescription,
                    lead.PatientCity,
                    lead.PatientRegion,
                    lead.BusinessName,
                    lead.AttendanceProbabilityPercent?.ToString() ?? string.Empty,
                    lead.SecondaryPhoneNumber));
            }
        }

        return CsvExportHelper.BuildFile(lines.ToArray());
    }

    private static bool IsLeadCalled(LeadAssignment lead) =>
        lead.ReportSubmittedAt.HasValue || lead.ContactedAt.HasValue || lead.CallResult.HasValue;
}
