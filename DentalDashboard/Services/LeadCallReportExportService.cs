using DentalDashboard.Infrastracture.Context;
using DentalDashboard.Utilities.Convertor;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Services;

public class LeadCallReportExportService
{
    private readonly DentalContext context;
    public LeadCallReportExportService(DentalContext context) => this.context = context;

    public async Task<byte[]> ExportCsvAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        var rows = await context.LeadAssignments.AsNoTracking()
            .Include(x => x.ConsultantProfile)!.ThenInclude(x => x.User)
            .Where(x => x.ReportSubmittedAt.HasValue && x.ReportSubmittedAt.Value >= from && x.ReportSubmittedAt.Value < to)
            .OrderBy(x => x.ReportSubmittedAt).ThenBy(x => x.Id)
            .Select(x => new
            {
                x.Id,
                LeadName = x.UserName,
                LeadPhone = x.PhoneNumber,
                x.PatientCity,
                x.PatientRegion,
                x.BusinessName,
                x.AttendanceProbabilityPercent,
                x.SecondaryPhoneNumber,
                x.CallResult,
                x.ReportDescription,
                x.ReportSubmittedAt,
                x.ContactedAt,
                ConsultantFullName = x.ConsultantProfile == null || x.ConsultantProfile.User == null
                    ? string.Empty
                    : x.ConsultantProfile.User.FirstName + " " + x.ConsultantProfile.User.LastName,
                ConsultantPhone = x.ConsultantProfile == null || x.ConsultantProfile.User == null
                    ? string.Empty
                    : x.ConsultantProfile.User.PhoneNumber,
                x.AssignmentType,
                x.LeadAssignmentState
            })
            .ToListAsync(cancellationToken);

        var lines = new List<string>
        {
            CsvExportHelper.JoinRow(
                "شناسه لید",
                "نام لید",
                "موبایل لید",
                "نام مشاور",
                "موبایل مشاور",
                "نتیجه تماس",
                "متن گزارش",
                "تاریخ ثبت گزارش",
                "تاریخ تماس",
                "شهر بیمار",
                "منطقه بیمار",
                "نام بیزینس",
                "احتمال حضور (درصد)",
                "شماره دوم بیمار",
                "نوع تخصیص",
                "وضعیت لید")
        };

        foreach (var row in rows)
        {
            lines.Add(CsvExportHelper.JoinRow(
                row.Id.ToString(),
                row.LeadName,
                row.LeadPhone,
                row.ConsultantFullName,
                row.ConsultantPhone,
                row.CallResult.HasValue ? row.CallResult.Value.ToPersian() : string.Empty,
                row.ReportDescription,
                row.ReportSubmittedAt.HasValue ? DateConvertor.ToPersianDateTimeString(row.ReportSubmittedAt.Value) : string.Empty,
                row.ContactedAt.HasValue ? DateConvertor.ToPersianDateTimeString(row.ContactedAt.Value) : string.Empty,
                row.PatientCity,
                row.PatientRegion,
                row.BusinessName,
                row.AttendanceProbabilityPercent?.ToString() ?? string.Empty,
                row.SecondaryPhoneNumber,
                row.AssignmentType.ToPersian(),
                row.LeadAssignmentState.ToPersian()));
        }

        return CsvExportHelper.BuildFile(lines.ToArray());
    }
}
