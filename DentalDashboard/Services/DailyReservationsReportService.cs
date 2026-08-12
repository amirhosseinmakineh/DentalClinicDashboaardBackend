using DentalDashboard.Domain.Enums;
using DentalDashboard.Infrastracture.Context;
using DentalDashboard.Utilities.Time;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Services;

public sealed record DailyReservationsReportSummary(
    int Total,
    int Active,
    int Canceled,
    int PendingSecretaryReview,
    int Confirmed,
    int Rescheduled,
    int Rejected,
    int UniqueConsultants);

public sealed record DailyReservationReportItem(
    long ReservationId,
    long LeadAssignmentId,
    long ConsultantProfileId,
    string ConsultantFullName,
    string ConsultantPhoneNumber,
    string PatientName,
    string PatientPhoneNumber,
    string? SecondaryPhoneNumber,
    string? PatientCity,
    string? PatientRegion,
    string? BusinessName,
    int? AttendanceProbabilityPercent,
    DateTime ReservationAt,
    DateTime CreatedAt,
    ReservationRequestStatus RequestStatus,
    string RequestStatusTitle,
    VisitResultStatus VisitResultStatus,
    string VisitResultStatusTitle,
    bool? IsConfirmedWithPatient,
    bool IsCanceled,
    string? CancellationReason,
    string? Description);

public sealed record DailyReservationsReport(
    DateOnly Date,
    DateTime GeneratedAt,
    DailyReservationsReportSummary Summary,
    IReadOnlyList<DailyReservationReportItem> Items);

public class DailyReservationsReportService(DentalContext context)
{
    public async Task<DailyReservationsReport> GetAsync(
        DateOnly? date,
        long? consultantProfileId,
        ReservationRequestStatus? requestStatus,
        CancellationToken cancellationToken = default)
    {
        var reportDate = date ?? IranTimeHelper.TodayInIran();
        var query = BuildQuery(reportDate, consultantProfileId, requestStatus);
        var items = await Project(query)
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.ReservationId)
            .ToListAsync(cancellationToken);

        return new DailyReservationsReport(
            reportDate,
            DateTime.UtcNow,
            new DailyReservationsReportSummary(
                items.Count,
                items.Count(x => !x.IsCanceled),
                items.Count(x => x.IsCanceled),
                items.Count(x => x.RequestStatus == ReservationRequestStatus.PendingSecretaryReview),
                items.Count(x => x.RequestStatus == ReservationRequestStatus.Confirmed),
                items.Count(x => x.RequestStatus == ReservationRequestStatus.Rescheduled),
                items.Count(x => x.RequestStatus == ReservationRequestStatus.Rejected),
                items.Select(x => x.ConsultantProfileId).Distinct().Count()),
            items);
    }

    public async Task<byte[]> ExportCsvAsync(
        DateOnly? date,
        long? consultantProfileId,
        ReservationRequestStatus? requestStatus,
        CancellationToken cancellationToken = default)
    {
        var report = await GetAsync(date, consultantProfileId, requestStatus, cancellationToken);
        var lines = new List<string>
        {
            CsvExportHelper.JoinRow(
                "شناسه رزرو", "شناسه لید", "شناسه مشاور", "نام مشاور", "موبایل مشاور",
                "نام بیمار", "موبایل بیمار", "شماره دوم", "شهر", "منطقه", "نام بیزینس",
                "احتمال حضور (درصد)", "تاریخ ثبت رزرو", "تاریخ مراجعه", "وضعیت درخواست",
                "نتیجه مراجعه", "تایید با بیمار", "لغو شده", "دلیل لغو", "توضیحات")
        };

        lines.AddRange(report.Items.Select(item => CsvExportHelper.JoinRow(
            item.ReservationId.ToString(),
            item.LeadAssignmentId.ToString(),
            item.ConsultantProfileId.ToString(),
            item.ConsultantFullName,
            item.ConsultantPhoneNumber,
            item.PatientName,
            item.PatientPhoneNumber,
            item.SecondaryPhoneNumber,
            item.PatientCity,
            item.PatientRegion,
            item.BusinessName,
            item.AttendanceProbabilityPercent?.ToString(),
            IranTimeHelper.ToIranLocalTime(item.CreatedAt).ToString("yyyy/MM/dd HH:mm"),
            IranTimeHelper.ToIranLocalTime(item.ReservationAt).ToString("yyyy/MM/dd HH:mm"),
            item.RequestStatusTitle,
            item.VisitResultStatusTitle,
            AdminReportPersianLabels.ToYesNoNullable(item.IsConfirmedWithPatient),
            AdminReportPersianLabels.ToYesNo(item.IsCanceled),
            item.CancellationReason,
            item.Description)));

        return CsvExportHelper.BuildFile(lines.ToArray());
    }

    private IQueryable<Domain.Models.Reservation> BuildQuery(
        DateOnly date,
        long? consultantProfileId,
        ReservationRequestStatus? requestStatus)
    {
        var (startUtc, _) = IranTimeHelper.GetIranDayRangeAsUtc(date);
        var (endUtc, _) = IranTimeHelper.GetIranDayRangeAsUtc(date.AddDays(1));
        var query = context.Reservations.AsNoTracking()
            .Where(x => !x.IsDeleted && x.CreatedAt >= startUtc && x.CreatedAt < endUtc);

        if (consultantProfileId.HasValue)
            query = query.Where(x => x.ConsultantProfileId == consultantProfileId.Value);
        if (requestStatus.HasValue)
            query = query.Where(x => x.ReservationRequestStatus == requestStatus.Value);

        return query;
    }

    private static IQueryable<DailyReservationReportItem> Project(IQueryable<Domain.Models.Reservation> query) =>
        query.Select(x => new DailyReservationReportItem(
            x.Id,
            x.LeadAssignmentId,
            x.ConsultantProfileId,
            x.ConsultantProfile.User.FirstName + " " + x.ConsultantProfile.User.LastName,
            x.ConsultantProfile.User.PhoneNumber,
            x.LeadAssignment.UserName,
            x.LeadAssignment.PhoneNumber,
            x.LeadAssignment.SecondaryPhoneNumber,
            x.LeadAssignment.PatientCity,
            x.LeadAssignment.PatientRegion,
            x.LeadAssignment.BusinessName,
            x.LeadAssignment.AttendanceProbabilityPercent,
            x.ReservationAt,
            x.CreatedAt,
            x.ReservationRequestStatus,
            ToPersian(x.ReservationRequestStatus),
            x.VisitResultStatus,
            ToPersian(x.VisitResultStatus),
            x.IsConfirmedWithPatient,
            x.IsCanceled,
            x.CancellationReason,
            x.Description));

    private static string ToPersian(ReservationRequestStatus status) => status switch
    {
        ReservationRequestStatus.PendingSecretaryReview => "در انتظار بررسی منشی",
        ReservationRequestStatus.Confirmed => "تایید شده",
        ReservationRequestStatus.Rescheduled => "زمان‌بندی مجدد",
        ReservationRequestStatus.Rejected => "رد شده",
        ReservationRequestStatus.Canceled => "لغو شده",
        ReservationRequestStatus.WaitingPatientConfirmation => "در انتظار تایید بیمار",
        ReservationRequestStatus.NeedsFollowUp => "نیازمند پیگیری",
        _ => "نامشخص"
    };

    private static string ToPersian(VisitResultStatus status) => status switch
    {
        VisitResultStatus.Pending => "در انتظار مراجعه",
        VisitResultStatus.Attended => "مراجعه کرده",
        VisitResultStatus.NoShow => "عدم مراجعه",
        VisitResultStatus.Canceled => "لغو شده",
        VisitResultStatus.NeedsFollowUp => "نیازمند پیگیری",
        _ => "نامشخص"
    };
}
