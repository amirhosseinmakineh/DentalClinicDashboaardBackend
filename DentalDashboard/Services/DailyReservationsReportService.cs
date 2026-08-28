using DentalDashboard.Domain.Enums;
using DentalDashboard.Infrastracture.Context;
using DentalDashboard.Utilities.Time;
using Microsoft.EntityFrameworkCore;
using DentalDashboard.Utilities.Convertor;

namespace DentalDashboard.Services;

public enum DailyReservationRequestStatus
{
    PendingSecretaryReview = 1,
    Confirmed = 2,
    Rescheduled = 3,
    Rejected = 4,
    Canceled = 5,
    WaitingPatientConfirmation = 6,
    NeedsFollowUp = 7
}

public enum DailyReservationVisitResultStatus
{
    Pending = 1,
    Attended = 2,
    NoShow = 3,
    Canceled = 4,
    NeedsFollowUp = 5
}

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
    int PatientCount,
    DateTime AppointmentDateTime,
    DateTime CreatedAt,
    DailyReservationRequestStatus RequestStatus,
    string RequestStatusTitle,
    DailyReservationVisitResultStatus VisitResultStatus,
    string VisitResultStatusTitle,
    bool? IsConfirmedWithPatient,
    bool IsCanceled,
    string? CancellationReason,
    string? Description,
    IReadOnlyList<DentalServiceType> DentalServices,
    SecretaryAnnouncementStatus? SecretaryAnnouncementStatus,
    string? SecretaryAnnouncementStatusTitle,
    string? SecretaryAnnouncement,
    DateTime? SecretaryAnnouncementUpdatedAt)
{
    public string AppointmentDateTimePersian =>
        AppointmentDateTime.ToPersianDateTimeString();
    public string CreatedAtPersian =>
        IranTimeHelper.ToIranLocalTime(CreatedAt).ToPersianDateTimeString();
}

public sealed record DailyReservationsReport(
    string Date,
    DateTime GeneratedAt,
    string? DatePersian,
    string GeneratedAtPersian,
    DailyReservationsReportSummary Summary,
    IReadOnlyList<DailyReservationReportItem> Items);

public class DailyReservationsReportService(DentalContext context)
{
    public async Task<DailyReservationsReport> GetAsync(
        DateOnly? date,
        ReservationOwnerType? reservationOwnerType,
        long? consultantProfileId,
        Guid? secretaryUserId,
        DailyReservationRequestStatus? requestStatus,
        bool includeAll = false,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.Now;
        var reportDate = date ?? DateOnly.FromDateTime(now);
        var query = BuildQuery(reportDate, reservationOwnerType, consultantProfileId,
            secretaryUserId, requestStatus, includeAll);
        var rows = await query
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Select(x => new
            {
                x.Id,
                x.LeadAssignmentId,
                x.ConsultantProfileId,
                ConsultantFullName = x.ConsultantProfile.User.FirstName + " " + x.ConsultantProfile.User.LastName,
                ConsultantPhoneNumber = x.ConsultantProfile.User.PhoneNumber,
                PatientName = x.LeadAssignment.UserName,
                PatientPhoneNumber = x.LeadAssignment.PhoneNumber,
                x.LeadAssignment.SecondaryPhoneNumber,
                x.LeadAssignment.PatientCity,
                x.LeadAssignment.PatientRegion,
                x.LeadAssignment.BusinessName,
                x.LeadAssignment.AttendanceProbabilityPercent,
                x.PatientCount,
                x.ReservationAt,
                x.CreatedAt,
                x.UpdatedAt,
                x.AttendanceConfirmationStatus,
                x.ConsultantSaysPatientAttended,
                x.IsCanceled,
                x.SecretaryReviewNote,
                x.Description,
                x.DentalServices,
                x.SecretaryAnnouncementStatus,
                x.SecretaryAnnouncement,
                x.SecretaryAnnouncementUpdatedAt
            })
            .ToListAsync(cancellationToken);

        var items = rows.Select(x =>
        {
            var status = GetRequestStatus(
                x.IsCanceled, x.UpdatedAt, x.AttendanceConfirmationStatus);
            var visitStatus = GetVisitResultStatus(
                x.IsCanceled, x.AttendanceConfirmationStatus, x.ConsultantSaysPatientAttended);
            return new DailyReservationReportItem(
                x.Id, x.LeadAssignmentId, x.ConsultantProfileId,
                x.ConsultantFullName.Trim(), x.ConsultantPhoneNumber,
                x.PatientName, x.PatientPhoneNumber, x.SecondaryPhoneNumber,
                x.PatientCity, x.PatientRegion, x.BusinessName,
                x.AttendanceProbabilityPercent, x.PatientCount,
                x.ReservationAt, EnsureUtc(x.CreatedAt),
                status, ToPersian(status), visitStatus, ToPersian(visitStatus),
                GetPatientConfirmation(x.AttendanceConfirmationStatus), x.IsCanceled,
                x.IsCanceled ? x.SecretaryReviewNote : null, x.Description, x.DentalServices,
                x.SecretaryAnnouncementStatus,
                x.SecretaryAnnouncementStatus.HasValue
                    ? ToPersian(x.SecretaryAnnouncementStatus.Value)
                    : null,
                x.SecretaryAnnouncement,
                x.SecretaryAnnouncementUpdatedAt.HasValue
                    ? EnsureUtc(x.SecretaryAnnouncementUpdatedAt.Value)
                    : null);
        }).ToList();

        return new DailyReservationsReport(
            includeAll ? string.Empty : reportDate.ToString("yyyy-MM-dd"),
            DateTime.UtcNow,
            includeAll ? null : reportDate.ToPersianDate(),
            IranTimeHelper.ToIranLocalTime(DateTime.UtcNow).ToPersianDateTimeString(),
            new DailyReservationsReportSummary(
                items.Count,
                items.Count(x => !x.IsCanceled),
                items.Count(x => x.IsCanceled),
                items.Count(x => x.RequestStatus == DailyReservationRequestStatus.PendingSecretaryReview),
                items.Count(x => x.RequestStatus == DailyReservationRequestStatus.Confirmed),
                items.Count(x => x.RequestStatus == DailyReservationRequestStatus.Rescheduled),
                items.Count(x => x.RequestStatus == DailyReservationRequestStatus.Rejected),
                items.Select(x => x.ConsultantProfileId).Distinct().Count()),
            items);
    }

    public async Task<byte[]> ExportCsvAsync(
        DateOnly? date,
        ReservationOwnerType? reservationOwnerType,
        long? consultantProfileId,
        Guid? secretaryUserId,
        DailyReservationRequestStatus? requestStatus,
        bool includeAll = false,
        CancellationToken cancellationToken = default)
    {
        var report = await GetAsync(
            date, reservationOwnerType, consultantProfileId, secretaryUserId,
            requestStatus, includeAll, cancellationToken);
        var lines = new List<string>
        {
            CsvExportHelper.JoinRow(
                "شناسه رزرو", "شناسه لید", "شناسه مشاور", "نام مشاور", "موبایل مشاور",
                "نام بیمار", "موبایل بیمار", "شماره دوم", "شهر", "منطقه", "نام بیزینس",
                "احتمال حضور (درصد)", "تعداد بیماران", "تاریخ ثبت رزرو", "تاریخ مراجعه", "وضعیت درخواست",
                "نتیجه مراجعه", "تایید با بیمار", "لغو شده", "دلیل لغو", "توضیحات", "خدمات",
                "وضعیت اعلام منشی", "اعلام منشی", "زمان اعلام منشی")
        };

        lines.AddRange(report.Items.Select(item => CsvExportHelper.JoinRow(
            item.ReservationId.ToString(), item.LeadAssignmentId.ToString(),
            item.ConsultantProfileId.ToString(), item.ConsultantFullName,
            item.ConsultantPhoneNumber, item.PatientName, item.PatientPhoneNumber,
            item.SecondaryPhoneNumber, item.PatientCity, item.PatientRegion, item.BusinessName,
            item.AttendanceProbabilityPercent?.ToString(), item.PatientCount.ToString(), FormatIran(item.CreatedAt),
            item.AppointmentDateTime.ToPersianDateTimeString(), item.RequestStatusTitle, item.VisitResultStatusTitle,
            AdminReportPersianLabels.ToYesNoNullable(item.IsConfirmedWithPatient),
            AdminReportPersianLabels.ToYesNo(item.IsCanceled), item.CancellationReason, item.Description,
            string.Join("، ", item.DentalServices.Select(ToPersian)),
            item.SecretaryAnnouncementStatusTitle, item.SecretaryAnnouncement,
            item.SecretaryAnnouncementUpdatedAt.HasValue
                ? FormatIran(item.SecretaryAnnouncementUpdatedAt.Value)
                : null)));

        return CsvExportHelper.BuildFile(lines.ToArray());
    }

    private IQueryable<Domain.Models.Reservation> BuildQuery(
     DateOnly date,
     ReservationOwnerType? reservationOwnerType,
     long? consultantProfileId,
     Guid? secretaryUserId,
     DailyReservationRequestStatus? requestStatus,
     bool includeAll)
    {
        var query = context.Reservations
            .AsNoTracking()
            .Where(x => !x.IsDeleted);

        if (includeAll)
            return query;

        // مقایسه مستقیم با تاریخ ذخیره‌شده در Backend/Database
        var startDate = date.ToDateTime(TimeOnly.MinValue);
        var endDate = date.AddDays(1).ToDateTime(TimeOnly.MinValue);

        if (reservationOwnerType.HasValue)
        {
            if (reservationOwnerType == ReservationOwnerType.Consultant)
            {
                query = query.Where(x =>
            x.CreatedAt >= startDate &&
            x.CreatedAt < endDate);
            }
            else
            {


                query = query.Where(x =>
                    x.ReservationAt >= startDate &&
                    x.ReservationAt < endDate);
            }
        }

        if (consultantProfileId.HasValue)
            query = query.Where(x =>
                x.ConsultantProfileId == consultantProfileId.Value);

        if (secretaryUserId.HasValue)
            query = query.Where(x =>
                x.OwnerUserId == secretaryUserId.Value);

        if (requestStatus.HasValue)
            query = ApplyRequestStatusFilter(query, requestStatus.Value);

        return query;
    }

    private static IQueryable<Domain.Models.Reservation> ApplyRequestStatusFilter(
        IQueryable<Domain.Models.Reservation> query, DailyReservationRequestStatus status) => status switch
        {
            DailyReservationRequestStatus.Canceled => query.Where(x => x.IsCanceled),
            DailyReservationRequestStatus.Rejected => query.Where(x => !x.IsCanceled && x.AttendanceConfirmationStatus == ReservationAttendanceConfirmationStatus.SecretaryRejected),
            DailyReservationRequestStatus.Confirmed => query.Where(x => !x.IsCanceled && x.AttendanceConfirmationStatus == ReservationAttendanceConfirmationStatus.SecretaryApproved),
            DailyReservationRequestStatus.Rescheduled => query.Where(x => !x.IsCanceled && x.UpdatedAt != null && x.AttendanceConfirmationStatus != ReservationAttendanceConfirmationStatus.SecretaryApproved && x.AttendanceConfirmationStatus != ReservationAttendanceConfirmationStatus.SecretaryRejected),
            DailyReservationRequestStatus.PendingSecretaryReview => query.Where(x => !x.IsCanceled && x.UpdatedAt == null && (x.AttendanceConfirmationStatus == ReservationAttendanceConfirmationStatus.ConsultantConfirmedPresent || x.AttendanceConfirmationStatus == ReservationAttendanceConfirmationStatus.ConsultantConfirmedAbsent)),
            DailyReservationRequestStatus.WaitingPatientConfirmation => query.Where(x => !x.IsCanceled && x.UpdatedAt == null && x.AttendanceConfirmationStatus == ReservationAttendanceConfirmationStatus.PendingConsultantConfirmation),
            DailyReservationRequestStatus.NeedsFollowUp => query.Where(x => false),
            _ => query
        };

    private static DailyReservationRequestStatus GetRequestStatus(
        bool isCanceled, DateTime? updatedAt, ReservationAttendanceConfirmationStatus attendanceStatus)
    {
        if (isCanceled) return DailyReservationRequestStatus.Canceled;
        if (attendanceStatus == ReservationAttendanceConfirmationStatus.SecretaryRejected) return DailyReservationRequestStatus.Rejected;
        if (attendanceStatus == ReservationAttendanceConfirmationStatus.SecretaryApproved) return DailyReservationRequestStatus.Confirmed;
        if (updatedAt.HasValue) return DailyReservationRequestStatus.Rescheduled;
        return attendanceStatus == ReservationAttendanceConfirmationStatus.PendingConsultantConfirmation
            ? DailyReservationRequestStatus.WaitingPatientConfirmation
            : DailyReservationRequestStatus.PendingSecretaryReview;
    }

    private static DailyReservationVisitResultStatus GetVisitResultStatus(
        bool isCanceled, ReservationAttendanceConfirmationStatus status, bool? attended)
    {
        if (isCanceled) return DailyReservationVisitResultStatus.Canceled;
        if (status == ReservationAttendanceConfirmationStatus.SecretaryRejected)
            return DailyReservationVisitResultStatus.NeedsFollowUp;
        if (attended == true) return DailyReservationVisitResultStatus.Attended;
        if (attended == false) return DailyReservationVisitResultStatus.NoShow;
        return DailyReservationVisitResultStatus.Pending;
    }

    private static bool? GetPatientConfirmation(ReservationAttendanceConfirmationStatus status) => status switch
    {
        ReservationAttendanceConfirmationStatus.SecretaryApproved => true,
        ReservationAttendanceConfirmationStatus.SecretaryRejected => false,
        _ => null
    };

    private static DateTime EnsureUtc(DateTime value) => value.Kind == DateTimeKind.Utc
        ? value
        : DateTime.SpecifyKind(value, DateTimeKind.Utc);

    private static string FormatIran(DateTime value) =>
        IranTimeHelper.ToIranLocalTime(value).ToPersianDateTimeString();

    private static string ToPersian(DailyReservationRequestStatus status) => status switch
    {
        DailyReservationRequestStatus.PendingSecretaryReview => "در انتظار بررسی منشی",
        DailyReservationRequestStatus.Confirmed => "تایید شده",
        DailyReservationRequestStatus.Rescheduled => "زمان‌بندی مجدد",
        DailyReservationRequestStatus.Rejected => "رد شده",
        DailyReservationRequestStatus.Canceled => "لغو شده",
        DailyReservationRequestStatus.WaitingPatientConfirmation => "در انتظار تایید بیمار",
        DailyReservationRequestStatus.NeedsFollowUp => "نیازمند پیگیری",
        _ => "نامشخص"
    };

    private static string ToPersian(DailyReservationVisitResultStatus status) => status switch
    {
        DailyReservationVisitResultStatus.Pending => "در انتظار مراجعه",
        DailyReservationVisitResultStatus.Attended => "مراجعه کرده",
        DailyReservationVisitResultStatus.NoShow => "عدم مراجعه",
        DailyReservationVisitResultStatus.Canceled => "لغو شده",
        DailyReservationVisitResultStatus.NeedsFollowUp => "نیازمند پیگیری",
        _ => "نامشخص"
    };

    private static string ToPersian(DentalServiceType service) => service switch
    {
        DentalServiceType.Composite => "کامپوزیت",
        DentalServiceType.Implant => "ایمپلنت",
        DentalServiceType.Laminate => "لمینت",
        _ => service.ToString()
    };

    private static string ToPersian(SecretaryAnnouncementStatus status) => status switch
    {
        SecretaryAnnouncementStatus.NotCalled => "تماس گرفته نشده",
        SecretaryAnnouncementStatus.NoAnswer => "پاسخ نداد",
        SecretaryAnnouncementStatus.Confirmed => "تایید کرد",
        SecretaryAnnouncementStatus.CancelledByPatient => "لغو توسط بیمار",
        SecretaryAnnouncementStatus.RescheduleRequested => "درخواست تغییر زمان",
        SecretaryAnnouncementStatus.CallAgain => "تماس مجدد",
        _ => "نامشخص"
    };
}
