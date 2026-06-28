using System.Globalization;
using System.Text;
using DentalDashboard.Infrastracture.Context;
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
            .Select(x => new { x.Id, LeadName = x.UserName, LeadPhone = x.PhoneNumber, x.PatientCity, x.PatientRegion, x.BusinessName, x.AttendanceProbabilityPercent, x.CallResult, x.ReportDescription, x.ReportSubmittedAt, x.ContactedAt, ConsultantFullName = x.ConsultantProfile == null ? string.Empty : x.ConsultantProfile.User.FirstName + " " + x.ConsultantProfile.User.LastName, ConsultantPhone = x.ConsultantProfile == null ? string.Empty : x.ConsultantProfile.User.PhoneNumber, x.AssignmentType, x.LeadAssignmentState })
            .ToListAsync(cancellationToken);

        var builder = new StringBuilder();
        builder.AppendLine("LeadId,LeadName,LeadPhone,ConsultantName,ConsultantPhone,CallResult,ReportDescription,ReportSubmittedAt,ContactedAt,PatientCity,PatientRegion,BusinessName,AttendanceProbabilityPercent,AssignmentType,LeadState");
        foreach (var row in rows)
            builder.AppendLine(string.Join(',', new[] { Csv(row.Id.ToString(CultureInfo.InvariantCulture)), Csv(row.LeadName), Csv(row.LeadPhone), Csv(row.ConsultantFullName), Csv(row.ConsultantPhone), Csv(row.CallResult?.ToString()), Csv(row.ReportDescription), Csv(row.ReportSubmittedAt?.ToString("O", CultureInfo.InvariantCulture)), Csv(row.ContactedAt?.ToString("O", CultureInfo.InvariantCulture)), Csv(row.PatientCity), Csv(row.PatientRegion), Csv(row.BusinessName), Csv(row.AttendanceProbabilityPercent?.ToString(CultureInfo.InvariantCulture)), Csv(row.AssignmentType.ToString()), Csv(row.LeadAssignmentState.ToString()) }));

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(builder.ToString())).ToArray();
    }

    private static string Csv(string? value) => $"\"{(value ?? string.Empty).Replace("\"", "\"\"")}\"";
}
