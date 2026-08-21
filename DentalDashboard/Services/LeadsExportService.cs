using DentalDashboard.Domain.Enums;
using DentalDashboard.Infrastracture.Context;
using DentalDashboard.Utilities.Convertor;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Services;

public sealed class LeadReportFilter
{
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    public long? ConsultantProfileId { get; set; }
    public LeadAssignmentState? LeadAssignmentState { get; set; }
    public LeadAssignmentType? AssignmentType { get; set; }
    public LeadCallResult? CallResult { get; set; }
    public bool? IsAssigned { get; set; }
    public bool? HasCalled { get; set; }
    public bool? HasSubmittedReport { get; set; }
    public string? SearchText { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}

public sealed record LeadReportSummary(
    int Total, int Assigned, int Unassigned, int Called, int NotCalled,
    int Converted, int WithSubmittedReport);

public sealed record LeadReportItem(
    long LeadId, string LeadName, string LeadPhoneNumber, string? SecondaryPhoneNumber,
    LeadAssignmentState LeadAssignmentState, string LeadAssignmentStateTitle,
    LeadAssignmentType AssignmentType, string AssignmentTypeTitle,
    long? ConsultantProfileId, string? ConsultantFullName, string? ConsultantPhoneNumber,
    bool IsAssigned, bool HasCalled, LeadCallResult? CallResult, string? CallResultTitle,
    string? ReportDescription, DateTime? AssignedAt, DateTime? ContactedAt,
    DateTime? ReportSubmittedAt, DateTime CreatedAt, string? PatientCity,
    string? PatientRegion, string? BusinessName, int? AttendanceProbabilityPercent)
{
    public string? AssignedAtPersian => Format(AssignedAt);
    public string? ContactedAtPersian => Format(ContactedAt);
    public string? ReportSubmittedAtPersian => Format(ReportSubmittedAt);
    public string CreatedAtPersian => DateConvertor.ToPersianDateTimeString(CreatedAt);

    private static string? Format(DateTime? value) =>
        value.HasValue ? DateConvertor.ToPersianDateTimeString(value.Value) : null;
}

public sealed record LeadsReport(
    LeadReportSummary Summary, IReadOnlyList<LeadReportItem> Items,
    int PageNumber, int PageSize, int TotalCount, int TotalPages);

public class LeadsExportService(DentalContext context)
{
    public async Task<LeadsReport> GetAsync(
        LeadReportFilter filter, CancellationToken cancellationToken = default)
    {
        Normalize(filter);
        var query = BuildQuery(filter);
        var totalCount = await query.CountAsync(cancellationToken);
        var summary = new LeadReportSummary(
            totalCount,
            await query.CountAsync(x => x.ConsultantProfileId != null, cancellationToken),
            await query.CountAsync(x => x.ConsultantProfileId == null, cancellationToken),
            await query.CountAsync(x => x.ReportSubmittedAt != null || x.ContactedAt != null, cancellationToken),
            await query.CountAsync(x => x.ReportSubmittedAt == null && x.ContactedAt == null, cancellationToken),
            await query.CountAsync(x => x.CallResult == LeadCallResult.Converted, cancellationToken),
            await query.CountAsync(x => x.ReportSubmittedAt != null, cancellationToken));

        var rows = await SelectItems(query)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        var items = rows.Select(ToReportItem).ToList();
        return new LeadsReport(summary, items, filter.PageNumber, filter.PageSize, totalCount,
            (int)Math.Ceiling((double)totalCount / filter.PageSize));
    }

    public async Task<byte[]> ExportCsvAsync(
        LeadReportFilter filter, CancellationToken cancellationToken = default)
    {
        Normalize(filter);
        var rows = await SelectItems(BuildQuery(filter)).ToListAsync(cancellationToken);
        var items = rows.Select(ToReportItem);
        var lines = new List<string>
        {
            CsvExportHelper.JoinRow(
                "شناسه لید", "نام لید", "موبایل لید", "شماره دوم", "وضعیت لید",
                "نوع تخصیص", "وضعیت اساین", "نام مشاور", "موبایل مشاور", "تاریخ تخصیص",
                "وضعیت تماس", "نتیجه تماس", "متن گزارش", "تاریخ ثبت گزارش", "تاریخ تماس",
                "شهر بیمار", "منطقه بیمار", "نام بیزینس", "احتمال حضور (درصد)", "تاریخ ایجاد لید")
        };

        lines.AddRange(items.Select(x => CsvExportHelper.JoinRow(
            x.LeadId.ToString(), x.LeadName, x.LeadPhoneNumber, x.SecondaryPhoneNumber,
            x.LeadAssignmentStateTitle, x.AssignmentTypeTitle,
            AdminReportPersianLabels.ToAssignmentStatus(x.ConsultantProfileId),
            x.ConsultantFullName, x.ConsultantPhoneNumber, x.AssignedAtPersian,
            AdminReportPersianLabels.ToCallStatus(x.HasCalled), x.CallResultTitle,
            x.ReportDescription, x.ReportSubmittedAtPersian, x.ContactedAtPersian,
            x.PatientCity, x.PatientRegion, x.BusinessName,
            x.AttendanceProbabilityPercent?.ToString(), x.CreatedAtPersian)));

        return CsvExportHelper.BuildFile(lines.ToArray());
    }

    private IQueryable<Domain.Models.LeadAssignment> BuildQuery(LeadReportFilter filter)
    {
        var query = context.LeadAssignments.AsNoTracking().Where(x => !x.IsDeleted);
        if (filter.From.HasValue)
        {
            var from = filter.From.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(x => x.CreatedAt >= from);
        }
        if (filter.To.HasValue)
        {
            var toExclusive = filter.To.Value.AddDays(1).ToDateTime(TimeOnly.MinValue);
            query = query.Where(x => x.CreatedAt < toExclusive);
        }
        if (filter.ConsultantProfileId.HasValue)
            query = query.Where(x => x.ConsultantProfileId == filter.ConsultantProfileId);
        if (filter.LeadAssignmentState.HasValue)
            query = query.Where(x => x.LeadAssignmentState == filter.LeadAssignmentState);
        if (filter.AssignmentType.HasValue)
            query = query.Where(x => x.AssignmentType == filter.AssignmentType);
        if (filter.CallResult.HasValue)
            query = query.Where(x => x.CallResult == filter.CallResult);
        if (filter.IsAssigned.HasValue)
            query = filter.IsAssigned.Value
                ? query.Where(x => x.ConsultantProfileId != null)
                : query.Where(x => x.ConsultantProfileId == null);
        if (filter.HasCalled.HasValue)
            query = filter.HasCalled.Value
                ? query.Where(x => x.ReportSubmittedAt != null || x.ContactedAt != null)
                : query.Where(x => x.ReportSubmittedAt == null && x.ContactedAt == null);
        if (filter.HasSubmittedReport.HasValue)
            query = filter.HasSubmittedReport.Value
                ? query.Where(x => x.ReportSubmittedAt != null)
                : query.Where(x => x.ReportSubmittedAt == null);
        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            var term = filter.SearchText.Trim();
            query = query.Where(x => x.UserName.Contains(term) || x.PhoneNumber.Contains(term) ||
                (x.SecondaryPhoneNumber != null && x.SecondaryPhoneNumber.Contains(term)) ||
                (x.BusinessName != null && x.BusinessName.Contains(term)));
        }
        return query;
    }

    private static IQueryable<LeadRow> SelectItems(IQueryable<Domain.Models.LeadAssignment> query) =>
        query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
            .Select(x => new LeadRow(
                x.Id, x.UserName, x.PhoneNumber, x.SecondaryPhoneNumber,
                x.LeadAssignmentState, x.AssignmentType, x.ConsultantProfileId,
                x.ConsultantProfile == null || x.ConsultantProfile.User == null ? null :
                    (x.ConsultantProfile.User.FirstName + " " + x.ConsultantProfile.User.LastName).Trim(),
                x.ConsultantProfile == null || x.ConsultantProfile.User == null ? null : x.ConsultantProfile.User.PhoneNumber,
                x.CallResult, x.ReportDescription, x.AssignedAt, x.ContactedAt,
                x.ReportSubmittedAt, x.CreatedAt, x.PatientCity, x.PatientRegion,
                x.BusinessName, x.AttendanceProbabilityPercent));

    private static LeadReportItem ToReportItem(LeadRow x) => new(
        x.Id, x.UserName, x.PhoneNumber, x.SecondaryPhoneNumber,
        x.State, x.State.ToPersian(), x.Type, x.Type.ToPersian(),
        x.ConsultantProfileId, x.ConsultantFullName, x.ConsultantPhone,
        x.ConsultantProfileId.HasValue, x.ReportSubmittedAt.HasValue || x.ContactedAt.HasValue,
        x.CallResult, x.CallResult?.ToPersian(), x.ReportDescription, x.AssignedAt,
        x.ContactedAt, x.ReportSubmittedAt, x.CreatedAt, x.PatientCity,
        x.PatientRegion, x.BusinessName, x.AttendanceProbabilityPercent);

    private static void Normalize(LeadReportFilter filter)
    {
        filter.PageNumber = Math.Max(1, filter.PageNumber);
        filter.PageSize = Math.Clamp(filter.PageSize, 1, 200);
        filter.SearchText = string.IsNullOrWhiteSpace(filter.SearchText) ? null : filter.SearchText.Trim();
    }

    private sealed record LeadRow(
        long Id, string UserName, string PhoneNumber, string? SecondaryPhoneNumber,
        LeadAssignmentState State, LeadAssignmentType Type, long? ConsultantProfileId,
        string? ConsultantFullName, string? ConsultantPhone, LeadCallResult? CallResult,
        string? ReportDescription, DateTime? AssignedAt, DateTime? ContactedAt,
        DateTime? ReportSubmittedAt, DateTime CreatedAt, string? PatientCity,
        string? PatientRegion, string? BusinessName, int? AttendanceProbabilityPercent);
}
