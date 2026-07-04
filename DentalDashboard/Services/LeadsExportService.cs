using DentalDashboard.Infrastracture.Context;
using DentalDashboard.Utilities.Convertor;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Services;

public class LeadsExportService
{
    private readonly DentalContext context;

    public LeadsExportService(DentalContext context) => this.context = context;

    public async Task<byte[]> ExportCsvAsync(CancellationToken cancellationToken = default)
    {
        var rows = await context.LeadAssignments.AsNoTracking()
            .Include(x => x.ConsultantProfile)!.ThenInclude(x => x.User)
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
            .Select(x => new
            {
                x.Id,
                x.UserName,
                x.PhoneNumber,
                x.LeadAssignmentState,
                x.AssignmentType,
                x.ConsultantProfileId,
                ConsultantFullName = x.ConsultantProfile == null || x.ConsultantProfile.User == null
                    ? string.Empty
                    : x.ConsultantProfile.User.FirstName + " " + x.ConsultantProfile.User.LastName,
                ConsultantPhone = x.ConsultantProfile == null || x.ConsultantProfile.User == null
                    ? string.Empty
                    : x.ConsultantProfile.User.PhoneNumber,
                x.AssignedAt,
                x.CallResult,
                x.ReportSubmittedAt,
                x.ContactedAt,
                x.CreatedAt,
                x.PatientCity,
                x.PatientRegion,
                x.BusinessName,
                x.AttendanceProbabilityPercent,
                x.SecondaryPhoneNumber
            })
            .ToListAsync(cancellationToken);

        var lines = new List<string>
        {
            CsvExportHelper.JoinRow(
                "شناسه لید",
                "نام لید",
                "موبایل لید",
                "وضعیت لید",
                "نوع تخصیص",
                "وضعیت اساین",
                "نام مشاور",
                "موبایل مشاور",
                "تاریخ تخصیص",
                "وضعیت تماس",
                "نتیجه تماس",
                "تاریخ ثبت گزارش",
                "تاریخ تماس",
                "شهر بیمار",
                "منطقه بیمار",
                "نام بیزینس",
                "احتمال حضور (درصد)",
                "شماره دوم بیمار",
                "تاریخ ایجاد لید")
        };

        foreach (var row in rows)
        {
            var hasCalled = row.ReportSubmittedAt.HasValue || row.ContactedAt.HasValue;

            lines.Add(CsvExportHelper.JoinRow(
                row.Id.ToString(),
                row.UserName,
                row.PhoneNumber,
                row.LeadAssignmentState.ToPersian(),
                row.AssignmentType.ToPersian(),
                AdminReportPersianLabels.ToAssignmentStatus(row.ConsultantProfileId),
                row.ConsultantFullName,
                row.ConsultantPhone,
                row.AssignedAt.HasValue ? DateConvertor.ToPersianDateTimeString(row.AssignedAt.Value) : string.Empty,
                AdminReportPersianLabels.ToCallStatus(hasCalled),
                row.CallResult.HasValue ? row.CallResult.Value.ToPersian() : string.Empty,
                row.ReportSubmittedAt.HasValue ? DateConvertor.ToPersianDateTimeString(row.ReportSubmittedAt.Value) : string.Empty,
                row.ContactedAt.HasValue ? DateConvertor.ToPersianDateTimeString(row.ContactedAt.Value) : string.Empty,
                row.PatientCity ?? string.Empty,
                row.PatientRegion ?? string.Empty,
                row.BusinessName ?? string.Empty,
                row.AttendanceProbabilityPercent?.ToString() ?? string.Empty,
                row.SecondaryPhoneNumber ?? string.Empty,
                DateConvertor.ToPersianDateTimeString(row.CreatedAt)));
        }

        return CsvExportHelper.BuildFile(lines.ToArray());
    }
}
