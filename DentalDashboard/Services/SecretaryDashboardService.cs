using System.Text.Json;
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
        catch (TimeZoneNotFoundException) when (id == "Asia/Tehran") { return TimeZoneInfo.FindSystemTimeZoneById("Iran Standard Time"); }
    }

    public async Task<SecretaryDashboardDto> GetAsync(DateOnly? date, string? timeZone, int listSize, CancellationToken ct)
    {
        var zone = ResolveTimeZone(timeZone); var now = DateTime.UtcNow;
        var businessDate = date ?? DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(now, zone));
        var local = businessDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        var start = TimeZoneInfo.ConvertTimeToUtc(local, zone); var end = TimeZoneInfo.ConvertTimeToUtc(local.AddDays(1), zone);
        listSize = Math.Clamp(listSize, 1, 20);
        var active = db.Reservations.AsNoTracking().Where(x => !x.IsDeleted && !x.IsCanceled);
        var pending = active.Where(x => x.ReservationRequestStatus == ReservationRequestStatus.PendingSecretaryReview);
        var today = active.Where(x => x.ReservationAt >= start && x.ReservationAt < end &&
            (x.ReservationRequestStatus == ReservationRequestStatus.Confirmed || x.ReservationRequestStatus == ReservationRequestStatus.Rescheduled));
        var due = db.ReservationFollowUps.AsNoTracking().Where(x => !x.IsDeleted && x.Status == FollowUpStatus.Pending &&
            !x.Reservation.IsDeleted && !x.Reservation.IsCanceled && ((x.ScheduledAt >= start && x.ScheduledAt < end) || (x.ReminderAt >= start && x.ReminderAt < end)));
        var counts = new DashboardCounts(await pending.CountAsync(ct), await today.CountAsync(ct),
            await due.Select(x => x.ReservationId).Distinct().CountAsync(ct),
            await active.CountAsync(x => x.ReservationAt < now && x.VisitResultStatus == VisitResultStatus.NoShow && x.VisitResultRecordedAt != null, ct));
        var todayItems = await Project(today.OrderBy(x => x.ReservationAt)).Take(listSize).ToListAsync(ct);
        var pendingItems = await Project(pending.OrderBy(x => x.CreatedAt)).Take(listSize).ToListAsync(ct);
        var upcoming = await Project(active.Where(x => x.ReservationAt >= end && (x.ReservationRequestStatus == ReservationRequestStatus.Confirmed || x.ReservationRequestStatus == ReservationRequestStatus.Rescheduled)).OrderBy(x => x.ReservationAt)).Take(listSize).ToListAsync(ct);
        var unconfirmed = await Project(active.Where(x => x.IsConfirmedWithPatient != true && x.ReservationAt >= now).OrderBy(x => x.ReservationAt)).Take(listSize).ToListAsync(ct);
        var followUps = await due.OrderByDescending(x => x.Priority).ThenBy(x => x.ScheduledAt).Take(listSize).Select(x => new FollowUpItemDto(x.Id, x.ReservationId, x.Reservation.PatientUserId, x.Reservation.LeadAssignment.UserName, x.Reservation.LeadAssignment.PhoneNumber, x.ScheduledAt, x.ReminderAt, x.Status, x.Priority, x.Reason, x.AssignedSecretaryUserId)).ToListAsync(ct);
        var activities = await db.SecretaryReservationActivities.AsNoTracking().OrderByDescending(x => x.CreatedAt).Take(listSize).Select(x => new ActivityItemDto(x.Id, x.ReservationId, x.Reservation.PatientUserId, x.Reservation.LeadAssignment.UserName, x.ActorUserId, x.ActorUser.FirstName + " " + x.ActorUser.LastName, x.ActivityType, x.Description, x.CreatedAt, x.PreviousValue, x.NewValue)).ToListAsync(ct);
        return new(now, businessDate, zone.Id, counts, todayItems, followUps, pendingItems, activities, upcoming, unconfirmed);
    }

    private static IQueryable<ReservationItemDto> Project(IQueryable<Reservation> q) => q.Select(x => new ReservationItemDto(x.Id, x.PatientUserId, x.LeadAssignment.UserName, x.LeadAssignment.PhoneNumber, x.ConsultantProfileId, x.ConsultantProfile.User.FirstName + " " + x.ConsultantProfile.User.LastName, x.ReservationAt, x.InitialReservationAt, x.ReservationRequestStatus, x.VisitResultStatus, x.IsConfirmedWithPatient, x.ConfirmedWithPatientAt, x.IsCanceled, x.LastActivityAt));

    public async Task<ReservationMutationDto> ReviewAsync(long id, Guid actor, ReservationRequestStatus status, DateTime? at, string? note, int? reasonCode, string? reason, CancellationToken ct)
    {
        var r = await Required(id, ct);
        if (r.ReservationRequestStatus != ReservationRequestStatus.PendingSecretaryReview) throw new InvalidOperationException("RESERVATION_ALREADY_REVIEWED");
        if (status == ReservationRequestStatus.Rejected)
        {
            if (!Enum.IsDefined(typeof(ReservationRejectionReason), reasonCode ?? 0)) throw new ArgumentException("REJECTION_REASON_REQUIRED");
            if ((ReservationRejectionReason)reasonCode! == ReservationRejectionReason.Other && string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("REJECTION_REASON_REQUIRED");
        }
        var previous = r.ReservationAt;
        if (status == ReservationRequestStatus.Rescheduled)
        {
            if (at is null || at <= DateTime.UtcNow) throw new ArgumentException("RESERVATION_TIME_IN_PAST");
            if (at == r.ReservationAt) throw new ArgumentException("RESERVATION_TIME_UNCHANGED");
            if (await db.Reservations.AnyAsync(x => x.Id != id && !x.IsDeleted && !x.IsCanceled && x.ReservationAt == at && (x.PatientUserId == r.PatientUserId || x.ConsultantProfileId == r.ConsultantProfileId), ct)) throw new InvalidOperationException("RESERVATION_TIME_CONFLICT");
            r.ReservationAt = at.Value;
        }
        var now = DateTime.UtcNow; r.ReservationRequestStatus = status; r.RequestReviewedAt = now; r.RequestReviewedByUserId = actor;
        r.RequestReviewNote = Clean(note, 1000); r.RejectionReasonCode = reasonCode; r.RejectionReason = Clean(reason, 1000); Touch(r, now);
        var eventType = status switch { ReservationRequestStatus.Confirmed => "ReservationConfirmed", ReservationRequestStatus.Rescheduled => "ReservationRescheduled", _ => "ReservationRejected" };
        var activity = AddActivity(r, actor, eventType, r.RequestReviewNote ?? r.RejectionReason ?? "وضعیت درخواست رزرو ثبت شد", status == ReservationRequestStatus.Rescheduled ? previous.ToString("O") : null, status == ReservationRequestStatus.Rescheduled ? r.ReservationAt.ToString("O") : null);
        await SaveAsync(ct); if (status != ReservationRequestStatus.Rejected) await QueueNotification(r, activity, eventType, ct);
        return new(r.Id, r.ReservationRequestStatus, r.ReservationAt, now);
    }

    public async Task SetPatientConfirmationAsync(long id, Guid actor, bool confirmed, string? note, CancellationToken ct)
    {
        var r = await Required(id, ct); var now = DateTime.UtcNow; r.IsConfirmedWithPatient = confirmed; r.ConfirmedWithPatientAt = now; r.ConfirmedWithPatientByUserId = actor;
        if (!confirmed && r.ReservationRequestStatus is ReservationRequestStatus.Confirmed or ReservationRequestStatus.Rescheduled) r.ReservationRequestStatus = ReservationRequestStatus.NeedsFollowUp;
        Touch(r, now); AddActivity(r, actor, "PatientConfirmationRecorded", Clean(note, 1000) ?? "هماهنگی بیمار ثبت شد"); await SaveAsync(ct);
    }

    public async Task SetVisitResultAsync(long id, Guid actor, VisitResultStatus status, string? note, CancellationToken ct)
    {
        var r = await Required(id, ct);
        if (r.ReservationRequestStatus is not (ReservationRequestStatus.Confirmed or ReservationRequestStatus.Rescheduled or ReservationRequestStatus.NeedsFollowUp)) throw new InvalidOperationException("RESERVATION_INVALID_STATUS");
        if (r.ReservationAt > DateTime.UtcNow) throw new ArgumentException("VISIT_RESULT_TOO_EARLY");
        if (!Enum.IsDefined(status)) throw new ArgumentException("INVALID_VISIT_RESULT");
        if (r.VisitResultRecordedAt != null && r.VisitResultStatus != VisitResultStatus.Pending) throw new InvalidOperationException("VISIT_RESULT_ALREADY_FINAL");
        var now = DateTime.UtcNow; r.VisitResultStatus = status; r.VisitResultRecordedAt = now; r.VisitResultRecordedByUserId = actor; r.VisitResultNote = Clean(note, 1000); Touch(r, now);
        AddActivity(r, actor, "VisitResultRecorded", r.VisitResultNote ?? "نتیجه مراجعه ثبت شد", null, status.ToString()); await SaveAsync(ct);
    }

    public async Task<FollowUpCreatedDto> CreateFollowUpAsync(long id, Guid actor, CreateFollowUpRequest request, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var r = await Required(id, ct);
        if (r.ReservationRequestStatus is ReservationRequestStatus.Rejected or ReservationRequestStatus.Canceled)
            throw new InvalidOperationException("RESERVATION_NOT_FOLLOWABLE");
        if (request.ScheduledAt <= DateTimeOffset.UtcNow) throw new ArgumentException("FOLLOW_UP_TIME_IN_PAST");
        var reason = Clean(request.Reason, 1000) ?? throw new ArgumentException("FOLLOW_UP_REASON_REQUIRED");
        var scheduledAt = request.ScheduledAt.UtcDateTime;
        var item = new ReservationFollowUp { ReservationId = id, ScheduledAt = scheduledAt, Priority = FollowUpPriority.Normal, Status = FollowUpStatus.Pending, Reason = reason, AssignedSecretaryUserId = actor, CreatedByUserId = actor };
        db.Add(item); Touch(r); var activity = AddActivity(r, actor, "FollowUpCreated", reason, null, scheduledAt.ToString("O")); await SaveAsync(ct);
        await QueueNotification(r, activity, "FollowUpCreated", ct);
        await transaction.CommitAsync(ct);
        return new(item.Id, id, scheduledAt, item.Status, item.Priority, scheduledAt, r.LastActivityAt);
    }

    public async Task UpdateFollowUpAsync(long id, long followUpId, Guid actor, FollowUpRequest request, CancellationToken ct)
    {
        var r = await Required(id, ct); var item = await db.ReservationFollowUps.FirstOrDefaultAsync(x => x.Id == followUpId && x.ReservationId == id && !x.IsDeleted, ct) ?? throw new KeyNotFoundException("FOLLOW_UP_NOT_FOUND");
        if (request.ScheduledAt <= DateTime.UtcNow && request.Status != FollowUpStatus.Completed) throw new ArgumentException("FOLLOW_UP_TIME_IN_PAST");
        item.ScheduledAt = request.ScheduledAt; item.ReminderAt = request.ReminderAt; item.Priority = request.Priority; item.Reason = Clean(request.Reason, 1000) ?? item.Reason; item.Status = request.Status ?? item.Status; item.UpdatedAt = DateTime.UtcNow;
        if (item.Status == FollowUpStatus.Completed) { item.CompletedAt = DateTime.UtcNow; item.CompletedByUserId = actor; }
        Touch(r); AddActivity(r, actor, item.Status == FollowUpStatus.Completed ? "FollowUpCompleted" : "ReservationReminderScheduled", item.Reason); await SaveAsync(ct);
    }

    public async Task<ContactCreatedDto> AddContactAsync(long id, Guid actor, ContactRequest request, CancellationToken ct)
    {
        var r = await Required(id, ct);
        if (!Enum.GetNames<ReservationContactResult>().Contains(request.Result, StringComparer.Ordinal) ||
            !Enum.TryParse<ReservationContactResult>(request.Result, ignoreCase: false, out var result))
            throw new ArgumentException("INVALID_CONTACT_RESULT");
        var contactedAt = DateTime.UtcNow;
        var entity = new ReservationContactLog { ReservationId = id, Result = result, Note = Clean(request.Note, 1000), CreatedByUserId = actor, CreatedAt = contactedAt };
        db.Add(entity); Touch(r, contactedAt); AddActivity(r, actor, "PatientContactLogged", result.ToString(), null, result.ToString()); await SaveAsync(ct);
        var callCount = await db.ReservationContactLogs.CountAsync(x => x.ReservationId == id && !x.IsDeleted, ct);
        return new(entity.Id, id, callCount, entity.Result.ToString(), contactedAt, r.LastActivityAt);
    }

    public async Task<NoteCreatedDto> AddNoteAsync(long id, Guid actor, string? note, CancellationToken ct)
    {
        var r = await Required(id, ct); var cleaned = Clean(note, 2000) ?? throw new ArgumentException("NOTE_REQUIRED"); var entity = new ReservationNote { ReservationId = id, Note = cleaned, CreatedByUserId = actor };
        db.Add(entity); Touch(r); AddActivity(r, actor, "ReservationNoteAdded", cleaned); await SaveAsync(ct);
        var actorName = await db.Users.Where(x => x.Id == actor).Select(x => x.FirstName + " " + x.LastName).FirstOrDefaultAsync(ct) ?? string.Empty;
        return new(entity.Id, id, entity.CreatedAt, actorName, r.LastActivityAt);
    }

    public async Task CancelAsync(long id, Guid actor, string? reason, CancellationToken ct)
    {
        var r = await Required(id, ct); var cleaned = Clean(reason, 1000) ?? throw new ArgumentException("CANCELLATION_REASON_REQUIRED"); var now = DateTime.UtcNow;
        r.IsCanceled = true; r.CanceledAt = now; r.CancellationReason = cleaned; r.ReservationRequestStatus = ReservationRequestStatus.Canceled; r.VisitResultStatus = VisitResultStatus.Canceled; Touch(r, now);
        var activity = AddActivity(r, actor, "ReservationCanceled", cleaned); await SaveAsync(ct); await QueueNotification(r, activity, "ReservationCanceled", ct);
    }

    public async Task<List<HistoryItemDto>> HistoryAsync(long id, CancellationToken ct)
    {
        if (!await db.Reservations.AsNoTracking().AnyAsync(x => x.Id == id && !x.IsDeleted, ct)) throw new KeyNotFoundException("RESERVATION_NOT_FOUND");
        return await db.SecretaryReservationActivities.AsNoTracking().Where(x => x.ReservationId == id)
            .OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).Take(200)
            .Select(x => new HistoryItemDto(x.Id, x.ActivityType, x.Description, x.CreatedAt, x.ActorUser.FirstName + " " + x.ActorUser.LastName, x.PreviousValue, x.NewValue)).ToListAsync(ct);
    }

    public async Task<PatientSecretaryViewDto> PatientAsync(Guid patientId, CancellationToken ct)
    {
        return await db.Reservations.AsNoTracking().Where(x => x.PatientUserId == patientId && !x.IsDeleted).OrderByDescending(x => x.CreatedAt).Select(x => new PatientSecretaryViewDto(patientId, x.LeadAssignment.UserName, x.LeadAssignment.PhoneNumber, x.Description, x.ConsultantProfile.User.FirstName + " " + x.ConsultantProfile.User.LastName, x.Id, x.ReservationAt, x.FollowUps.OrderByDescending(f => f.CreatedAt).Select(f => (DateTime?)f.ScheduledAt).FirstOrDefault(), x.ContactLogs.OrderByDescending(c => c.CreatedAt).Select(c => (ReservationContactResult?)c.Result).FirstOrDefault())).FirstOrDefaultAsync(ct) ?? throw new KeyNotFoundException("PATIENT_NOT_FOUND");
    }

    private async Task<Reservation> Required(long id, CancellationToken ct) => await db.Reservations.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted && !x.IsCanceled, ct) ?? throw new KeyNotFoundException("RESERVATION_NOT_FOUND");
    private static string? Clean(string? value, int max) { if (string.IsNullOrWhiteSpace(value)) return null; var result = value.Trim(); if (result.Length > max) throw new ArgumentException("TEXT_TOO_LONG"); return result; }
    private static void Touch(Reservation r, DateTime? now = null) { var time = now ?? DateTime.UtcNow; r.UpdatedAt = time; r.LastActivityAt = time; }
    private SecretaryReservationActivity AddActivity(Reservation r, Guid actor, string type, string description, string? previous = null, string? next = null) { var a = new SecretaryReservationActivity { ReservationId = r.Id, ActorUserId = actor, ActivityType = type, Description = description, PreviousValue = previous, NewValue = next }; db.Add(a); return a; }
    private async Task QueueNotification(Reservation r, SecretaryReservationActivity a, string eventType, CancellationToken ct) { db.Add(new ReservationNotificationOutbox { ReservationId = r.Id, ActivityId = a.Id, EventType = eventType, IdempotencyKey = $"{r.Id}:{a.Id}:{eventType}", Payload = JsonSerializer.Serialize(new { reservationId = r.Id, activityId = a.Id, eventType, reservationAt = r.ReservationAt }) }); await SaveAsync(ct); }
    private async Task SaveAsync(CancellationToken ct) { try { await db.SaveChangesAsync(ct); } catch (DbUpdateConcurrencyException) { throw new InvalidOperationException("CONCURRENCY_CONFLICT"); } }
}

public enum ReservationRejectionReason { NoCapacity = 1, PatientNoAnswer = 2, RequestedCancellation = 3, Other = 4 }
public record DashboardCounts(int PendingReservationRequests, int ConfirmedTodayReservations, int TodayFollowUps, int NoShows);
public record SecretaryDashboardDto(DateTime GeneratedAt, DateOnly BusinessDate, string TimeZone, DashboardCounts Counts, IReadOnlyList<ReservationItemDto> TodayReservations, IReadOnlyList<FollowUpItemDto> PriorityFollowUps, IReadOnlyList<ReservationItemDto> PendingReservationRequests, IReadOnlyList<ActivityItemDto> RecentSecretaryActivities, IReadOnlyList<ReservationItemDto> UpcomingReservations, IReadOnlyList<ReservationItemDto> UnconfirmedWithPatientReservations);
public record ReservationItemDto(long Id, Guid? PatientUserId, string PatientName, string PatientPhoneNumber, long ConsultantProfileId, string ConsultantFullName, DateTime ReservationAt, DateTime InitialReservationAt, ReservationRequestStatus ReservationRequestStatus, VisitResultStatus VisitResultStatus, bool? IsConfirmedWithPatient, DateTime? ConfirmedWithPatientAt, bool IsCanceled, DateTime LastActivityAt);
public record FollowUpItemDto(long FollowUpId, long ReservationId, Guid? PatientUserId, string PatientName, string PatientPhoneNumber, DateTime ScheduledAt, DateTime? ReminderAt, FollowUpStatus Status, FollowUpPriority Priority, string Reason, Guid? AssignedSecretaryUserId);
public record ActivityItemDto(long ActivityId, long ReservationId, Guid? PatientUserId, string PatientName, Guid ActorUserId, string ActorDisplayName, string ActivityType, string Description, DateTime CreatedAt, string? PreviousValue, string? NewValue);
public record ReservationMutationDto(long ReservationId, ReservationRequestStatus ReservationRequestStatus, DateTime ReservationAt, DateTime ReviewedAt);
public record FollowUpRequest(DateTime ScheduledAt, DateTime? ReminderAt, FollowUpPriority Priority, string? Reason, Guid? AssignedSecretaryUserId, FollowUpStatus? Status = null);
public record CreateFollowUpRequest(DateTimeOffset ScheduledAt, string? Reason);
public record ContactRequest(string Result, string? Note);
public record ContactCreatedDto(long ContactId, long ReservationId, int CallCount, string LastContactResult, DateTime ContactedAt, DateTime LastActivityAt);
public record NoteCreatedDto(long NoteId, long ReservationId, DateTime CreatedAt, string CreatedByDisplayName, DateTime LastActivityAt);
public record FollowUpCreatedDto(long FollowUpId, long ReservationId, DateTime ScheduledAt, FollowUpStatus Status, FollowUpPriority Priority, DateTime LastFollowUpAt, DateTime LastActivityAt);
public record HistoryItemDto(long ActivityId, string ActivityType, string Description, DateTime CreatedAt, string ActorDisplayName, string? PreviousValue, string? NewValue);
public record PatientSecretaryViewDto(Guid PatientUserId, string PatientName, string PatientPhoneNumber, string? RequestedServiceName, string ConsultantFullName, long ReservationId, DateTime ReservationAt, DateTime? LastFollowUpAt, ReservationContactResult? LastContactResult);
