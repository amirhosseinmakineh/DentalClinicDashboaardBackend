using DentalDashboard.Domain.Models;
using DentalDashboard.Infrastracture.Context;
using DentalDashboard.Utilities.Convertor;
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
            .OrderByDescending(x => x.CurrentScore).ThenBy(x => x.User!.LastName)
            .ToListAsync(cancellationToken);

        var consultantIds = consultants.Select(x => x.Id).ToList();

        var leadAssignments = await context.LeadAssignments.AsNoTracking()
            .Where(x => !x.IsDeleted && x.ConsultantProfileId.HasValue && consultantIds.Contains(x.ConsultantProfileId.Value))
            .OrderBy(x => x.ConsultantProfileId).ThenByDescending(x => x.AssignedAt)
            .ToListAsync(cancellationToken);

        var leadsByConsultant = leadAssignments
            .GroupBy(x => x.ConsultantProfileId!.Value)
            .ToDictionary(x => x.Key, x => x.ToList());

        var lines = new List<string>
        {
            CsvExportHelper.JoinRow("بخش خلاصه مشاوران"),
            CsvExportHelper.JoinRow(
                "شناسه مشاور",
                "نام",
                "نام خانوادگی",
                "موبایل",
                "کد ملی",
                "امتیاز فعلی",
                "وضعیت آنلاین",
                "وضعیت حضور",
                "تعداد کل لیدها",
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

            lines.Add(CsvExportHelper.JoinRow(
                consultant.Id.ToString(),
                consultant.User?.FirstName ?? string.Empty,
                consultant.User?.LastName ?? string.Empty,
                consultant.User?.PhoneNumber ?? string.Empty,
                consultant.NationalCode,
                consultant.CurrentScore.ToString(),
                AdminReportPersianLabels.ToYesNo(consultant.IsOnline),
                AdminReportPersianLabels.ToYesNo(consultant.IsAvailable),
                leads.Count.ToString(),
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
                    lead.LeadAssignmentState.ToPersian(),
                    lead.AssignmentType.ToPersian(),
                    lead.AssignedAt.HasValue ? DateConvertor.ToPersianDateTimeString(lead.AssignedAt.Value) : string.Empty,
                    AdminReportPersianLabels.ToCallStatus(hasCalled),
                    lead.CallResult.HasValue ? lead.CallResult.Value.ToPersian() : string.Empty,
                    lead.ContactedAt.HasValue ? DateConvertor.ToPersianDateTimeString(lead.ContactedAt.Value) : string.Empty,
                    lead.ReportSubmittedAt.HasValue ? DateConvertor.ToPersianDateTimeString(lead.ReportSubmittedAt.Value) : string.Empty,
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
