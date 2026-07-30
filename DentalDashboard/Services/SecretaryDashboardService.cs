using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.Models;
using DentalDashboard.Infrastracture.Context;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Services;

public sealed class SecretaryDashboardService(DentalContext db)
{
    public static TimeZoneInfo ResolveTimeZone(string? id)
    {
        id = string.IsNullOrWhiteSpace(id) ? "Asia/Tehran" : id;
        try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
        catch (TimeZoneNotFoundException) when (id == "Asia/Tehran")
        { return TimeZoneInfo.FindSystemTimeZoneById("Iran Standard Time"); }
    }

    public async Task<SecretaryDashboardDto> GetAsync(DateOnly? date, string? timeZone, int listSize, CancellationToken ct)
    {
        var zone = ResolveTimeZone(timeZone);
        var now = DateTime.UtcNow;
        var businessDate = date ?? DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(now, zone));
        var startLocal = businessDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        var endLocal = startLocal.AddDays(1);
        var start = TimeZoneInfo.ConvertTimeToUtc(startLocal, zone);
        var end = TimeZoneInfo.ConvertTimeToUtc(endLocal, zone);
        listSize = Math.Clamp(listSize, 1, 20);

        var active = db.Reservations.AsNoTracking().Where(x => !x.IsDeleted && !x.IsCanceled);
        var pending = active.Where(x => x.ReservationRequestStatus == ReservationRequestStatus.PendingSecretaryReview);
        var today = active.Where(x => x.ReservationAt >= start && x.ReservationAt < end &&
            (x.ReservationRequestStatus == ReservationRequestStatus.Confirmed || x.ReservationRequestStatus == ReservationRequestStatus.Rescheduled));
        var dueFollowUps = db.ReservationFollowUps.AsNoTracking().Where(x => !x.IsDeleted && x.Status == FollowUpStatus.Pending &&
            !x.Reservation.IsDeleted && !x.Reservation.IsCanceled &&
            ((x.ScheduledAt >= start && x.ScheduledAt < end) || (x.ReminderAt >= start && x.ReminderAt < end) ||
             (x.Reservation.VisitResultStatus == VisitResultStatus.NeedsFollowUp && x.ScheduledAt < end)));

        var counts = new DashboardCounts(
            await pending.CountAsync(ct), await today.CountAsync(ct),
            await dueFollowUps.Select(x => x.ReservationId).Distinct().CountAsync(ct),
            await active.CountAsync(x => x.ReservationAt < now && x.VisitResultStatus == VisitResultStatus.NoShow && x.VisitResultRecordedAt != null, ct));

        var todayItems = await ProjectReservations(today.OrderBy(x => x.ReservationAt)).Take(listSize).ToListAsync(ct);
        var pendingItems = await ProjectReservations(pending.OrderBy(x => x.CreatedAt)).Take(listSize).ToListAsync(ct);
        var upcoming = await ProjectReservations(active.Where(x => x.ReservationAt >= end &&
            (x.ReservationRequestStatus == ReservationRequestStatus.Confirmed || x.ReservationRequestStatus == ReservationRequestStatus.Rescheduled))
            .OrderBy(x => x.ReservationAt)).Take(listSize).ToListAsync(ct);
        var unconfirmed = await ProjectReservations(active.Where(x => !x.IsConfirmedWithPatient && x.ReservationAt >= now)
            .OrderBy(x => x.ReservationAt)).Take(listSize).ToListAsync(ct);
        var followUps = await dueFollowUps.OrderByDescending(x => x.Priority).ThenBy(x => x.ScheduledAt).Take(listSize)
            .Select(x => new FollowUpItemDto(x.Id, x.ReservationId, x.Reservation.PatientUserId,
                x.Reservation.LeadAssignment.UserName, x.Reservation.LeadAssignment.PhoneNumber, x.ScheduledAt,
                x.ReminderAt, x.Status, x.Priority, x.Reason, x.AssignedSecretaryUserId)).ToListAsync(ct);
        var activities = await db.SecretaryReservationActivities.AsNoTracking().OrderByDescending(x => x.CreatedAt).Take(listSize)
            .Select(x => new ActivityItemDto(x.Id, x.ReservationId, x.Reservation.PatientUserId,
                x.Reservation.LeadAssignment.UserName, x.ActorUserId, x.ActorUser.FirstName + " " + x.ActorUser.LastName,
                x.ActivityType, x.Description, x.CreatedAt)).ToListAsync(ct);

        return new(now, businessDate, zone.Id, counts, todayItems, followUps, pendingItems, activities, upcoming, unconfirmed);
    }

    private static IQueryable<ReservationItemDto> ProjectReservations(IQueryable<Reservation> query) => query.Select(x =>
        new ReservationItemDto(x.Id, x.PatientUserId, x.LeadAssignment.UserName, x.LeadAssignment.PhoneNumber,
            x.ConsultantProfileId, x.ConsultantProfile.User.FirstName + " " + x.ConsultantProfile.User.LastName,
            x.ReservationAt, x.ReservationRequestStatus, x.VisitResultStatus, x.IsConfirmedWithPatient,
            x.ConfirmedWithPatientAt, x.IsCanceled, x.UpdatedAt ?? x.CreatedAt));

    public async Task<ReservationMutationDto> ReviewAsync(long id, Guid actor, ReservationRequestStatus status,
        DateTime? reservationAt, string? note, int? reasonCode, string? reason, CancellationToken ct)
    {
        var reservation = await db.Reservations.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct)
            ?? throw new KeyNotFoundException("رزرو یافت نشد");
        if (reservation.ReservationRequestStatus != ReservationRequestStatus.PendingSecretaryReview)
            throw new InvalidOperationException("رزرو قبلاً توسط کاربر دیگری بررسی شده است");
        if (status == ReservationRequestStatus.Rejected && string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("دلیل رد اجباری است");
        if (status == ReservationRequestStatus.Rescheduled)
        {
            if (reservationAt is null || reservationAt <= DateTime.UtcNow) throw new ArgumentException("زمان جدید باید در آینده باشد");
            var collision = await db.Reservations.AnyAsync(x => x.Id != id && x.ConsultantProfileId == reservation.ConsultantProfileId &&
                x.ReservationAt == reservationAt && !x.IsCanceled && !x.IsDeleted, ct);
            if (collision) throw new InvalidOperationException("ظرفیت زمان انتخاب‌شده تکمیل است");
            reservation.ReservationAt = reservationAt.Value;
        }
        reservation.ReservationRequestStatus = status;
        reservation.RequestReviewedAt = DateTime.UtcNow;
        reservation.RequestReviewedByUserId = actor;
        reservation.RequestReviewNote = note;
        reservation.RejectionReasonCode = reasonCode;
        reservation.RejectionReason = reason;
        reservation.UpdatedAt = DateTime.UtcNow;
        AddActivity(reservation.Id, actor, status switch { ReservationRequestStatus.Confirmed => "ReservationConfirmed", ReservationRequestStatus.Rescheduled => "ReservationRescheduled", _ => "ReservationRejected" }, note ?? reason ?? "وضعیت درخواست رزرو ثبت شد");
        await SaveAsync(ct);
        return new(reservation.Id, reservation.ReservationRequestStatus, reservation.ReservationAt, reservation.RequestReviewedAt.Value);
    }

    public async Task SetPatientConfirmationAsync(long id, Guid actor, bool confirmed, string? note, CancellationToken ct)
    {
        var r = await RequiredReservation(id, ct); r.IsConfirmedWithPatient = confirmed;
        r.ConfirmedWithPatientAt = confirmed ? DateTime.UtcNow : null; r.PatientConfirmationNote = note; r.UpdatedAt = DateTime.UtcNow;
        AddActivity(id, actor, confirmed ? "PatientContactConfirmed" : "PatientContactLogged", note ?? "هماهنگی بیمار ثبت شد");
        await SaveAsync(ct);
    }

    public async Task SetVisitResultAsync(long id, Guid actor, VisitResultStatus status, string? note, CancellationToken ct)
    {
        var r = await RequiredReservation(id, ct);
        if (r.ReservationAt > DateTime.UtcNow && status is VisitResultStatus.Attended or VisitResultStatus.NoShow)
            throw new ArgumentException("نتیجه مراجعه پیش از زمان رزرو قابل ثبت نیست");
        r.VisitResultStatus = status; r.VisitResultRecordedAt = DateTime.UtcNow; r.VisitResultRecordedByUserId = actor; r.VisitResultNote = note; r.UpdatedAt = DateTime.UtcNow;
        AddActivity(id, actor, "VisitResultRecorded", note ?? "نتیجه مراجعه ثبت شد"); await SaveAsync(ct);
    }

    public async Task<long> CreateFollowUpAsync(long id, Guid actor, FollowUpRequest request, CancellationToken ct)
    {
        await RequiredReservation(id, ct);
        if (request.ScheduledAt == default) throw new ArgumentException("زمان پیگیری اجباری است");
        var item = new ReservationFollowUp { ReservationId = id, ScheduledAt = request.ScheduledAt, ReminderAt = request.ReminderAt,
            Priority = request.Priority, Reason = request.Reason?.Trim() ?? string.Empty, AssignedSecretaryUserId = request.AssignedSecretaryUserId ?? actor };
        if (string.IsNullOrWhiteSpace(item.Reason)) throw new ArgumentException("دلیل پیگیری اجباری است");
        db.Add(item); AddActivity(id, actor, "FollowUpCreated", item.Reason); await SaveAsync(ct); return item.Id;
    }

    public async Task UpdateFollowUpAsync(long id, long followUpId, Guid actor, FollowUpRequest request, CancellationToken ct)
    {
        var item = await db.ReservationFollowUps.FirstOrDefaultAsync(x => x.Id == followUpId && x.ReservationId == id && !x.IsDeleted, ct)
            ?? throw new KeyNotFoundException("پیگیری یافت نشد");
        item.ScheduledAt = request.ScheduledAt; item.ReminderAt = request.ReminderAt; item.Priority = request.Priority;
        item.Reason = request.Reason?.Trim() ?? item.Reason; item.Status = request.Status ?? item.Status;
        item.CompletedAt = item.Status == FollowUpStatus.Completed ? DateTime.UtcNow : null; item.UpdatedAt = DateTime.UtcNow;
        AddActivity(id, actor, item.Status == FollowUpStatus.Completed ? "FollowUpCompleted" : "ReminderScheduled", item.Reason); await SaveAsync(ct);
    }

    private async Task<Reservation> RequiredReservation(long id, CancellationToken ct) =>
        await db.Reservations.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted && !x.IsCanceled, ct) ?? throw new KeyNotFoundException("رزرو یافت نشد");
    private void AddActivity(long id, Guid actor, string type, string description) => db.Add(new SecretaryReservationActivity { ReservationId = id, ActorUserId = actor, ActivityType = type, Description = description });
    private async Task SaveAsync(CancellationToken ct) { try { await db.SaveChangesAsync(ct); } catch (DbUpdateConcurrencyException) { throw new InvalidOperationException("رزرو قبلاً توسط کاربر دیگری تغییر کرده است"); } }
}

public record DashboardCounts(int PendingReservationRequests, int ConfirmedTodayReservations, int TodayFollowUps, int NoShows);
public record SecretaryDashboardDto(DateTime GeneratedAt, DateOnly BusinessDate, string TimeZone, DashboardCounts Counts,
    IReadOnlyList<ReservationItemDto> TodayReservations, IReadOnlyList<FollowUpItemDto> PriorityFollowUps,
    IReadOnlyList<ReservationItemDto> PendingReservationRequests, IReadOnlyList<ActivityItemDto> RecentSecretaryActivities,
    IReadOnlyList<ReservationItemDto> UpcomingReservations, IReadOnlyList<ReservationItemDto> UnconfirmedWithPatientReservations);
public record ReservationItemDto(long ReservationId, Guid? PatientUserId, string PatientName, string PatientPhoneNumber,
    long ConsultantProfileId, string ConsultantFullName, DateTime ReservationAt, ReservationRequestStatus ReservationRequestStatus,
    VisitResultStatus VisitResultStatus, bool IsConfirmedWithPatient, DateTime? ConfirmedWithPatientAt, bool IsCanceled, DateTime LastActivityAt);
public record FollowUpItemDto(long FollowUpId, long ReservationId, Guid? PatientUserId, string PatientName, string PatientPhoneNumber,
    DateTime ScheduledAt, DateTime? ReminderAt, FollowUpStatus Status, FollowUpPriority Priority, string Reason, Guid? AssignedSecretaryUserId);
public record ActivityItemDto(long ActivityId, long ReservationId, Guid? PatientUserId, string PatientName, Guid ActorUserId,
    string ActorDisplayName, string ActivityType, string Description, DateTime CreatedAt);
public record ReservationMutationDto(long ReservationId, ReservationRequestStatus ReservationRequestStatus, DateTime ReservationAt, DateTime ReviewedAt);
public record FollowUpRequest(DateTime ScheduledAt, DateTime? ReminderAt, FollowUpPriority Priority, string? Reason, Guid? AssignedSecretaryUserId, FollowUpStatus? Status = null);
