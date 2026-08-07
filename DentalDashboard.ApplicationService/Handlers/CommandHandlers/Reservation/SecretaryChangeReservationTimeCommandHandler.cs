using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.ApplicationService.Contract.Requests.Reservation.Commands;
using DentalDashboard.ApplicationService.Contract.Responses.ReservationResponse;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Models;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using DentalDashboard.Framwork.IRepositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.Reservation;

public class SecretaryChangeReservationTimeCommandHandler : ICommandHandler<SecretaryChangeReservationTimeCommand, ReservationTimeChangeResponse>
{
    private const int MaxReservationsPerConsultantAtSameTime = 10;
    private readonly IReservationRepository reservations;
    private readonly IBaseRepository<long, ReservationTimeChange> changes;
    private readonly IBaseRepository<long, UserNotification> notifications;
    private readonly IUnitOfWork unitOfWork;
    private readonly IPushNotificationService push;
    private readonly ILogger<SecretaryChangeReservationTimeCommandHandler> logger;

    public SecretaryChangeReservationTimeCommandHandler(IReservationRepository reservations,
        IBaseRepository<long, ReservationTimeChange> changes, IBaseRepository<long, UserNotification> notifications,
        IUnitOfWork unitOfWork, IPushNotificationService push, ILogger<SecretaryChangeReservationTimeCommandHandler> logger)
    {
        this.reservations = reservations; this.changes = changes; this.notifications = notifications;
        this.unitOfWork = unitOfWork; this.push = push; this.logger = logger;
    }

    public async Task<Result<ReservationTimeChangeResponse>> HandleAsync(SecretaryChangeReservationTimeCommand command, CancellationToken cancellationToken = default)
    {
        if (command.SecretaryUserId == Guid.Empty || command.SecretaryUserId != command.AuthenticatedUserId)
            return Result<ReservationTimeChangeResponse>.Failure("شناسه منشی با کاربر واردشده مطابقت ندارد");
        if (command.Note?.Trim().Length > 1000)
            return Result<ReservationTimeChangeResponse>.Failure("توضیح منشی حداکثر ۱۰۰۰ کاراکتر است");
        if (command.NewReservationAt <= DateTime.UtcNow)
            return Result<ReservationTimeChangeResponse>.Failure("زمان جدید رزرو باید در آینده باشد");

        var reservation = await reservations.GetAll().Include(x => x.ConsultantProfile)
            .SingleOrDefaultAsync(x => x.Id == command.ReservationId, cancellationToken);
        if (reservation == null || reservation.IsDeleted || reservation.IsCanceled)
            return Result<ReservationTimeChangeResponse>.Failure("رزرو فعال یافت نشد");
        if (reservation.ReservationAt == command.NewReservationAt)
            return Result<ReservationTimeChangeResponse>.Failure("زمان جدید با زمان فعلی رزرو یکسان است");
        if (reservation.AttendanceConfirmationStatus is ReservationAttendanceConfirmationStatus.SecretaryApproved or ReservationAttendanceConfirmationStatus.SecretaryRejected)
            return Result<ReservationTimeChangeResponse>.Failure("بررسی حضور این رزرو نهایی شده است");
        if (await changes.GetAll().AnyAsync(x => x.ReservationId == reservation.Id && x.Status == ReservationTimeChangeStatus.PendingConsultantConfirmation, cancellationToken))
            return Result<ReservationTimeChangeResponse>.Failure("این رزرو در انتظار تایید تغییر زمان توسط مشاور است");
        if (await reservations.CountActiveReservationsAtExcludingAsync(reservation.ConsultantProfileId, command.NewReservationAt, reservation.Id) >= MaxReservationsPerConsultantAtSameTime)
            return Result<ReservationTimeChangeResponse>.Failure("ظرفیت این بازه زمانی برای مشاور تکمیل است");

        var now = DateTime.UtcNow;
        var change = new ReservationTimeChange { ReservationId = reservation.Id, PreviousReservationAt = reservation.ReservationAt,
            NewReservationAt = command.NewReservationAt, ChangedBySecretaryUserId = command.SecretaryUserId,
            Note = string.IsNullOrWhiteSpace(command.Note) ? null : command.Note.Trim(), Status = ReservationTimeChangeStatus.PendingConsultantConfirmation, CreatedAt = now };
        var notification = new UserNotification { UserId = reservation.ConsultantProfile.UserId, Type = "SecretaryReservationTimeChanged",
            Title = "زمان رزرو توسط منشی تغییر کرد", Body = "زمان رزرو بیمار به تاریخ و ساعت جدید تغییر کرده است",
            ReservationId = reservation.Id, Route = "/consultant-dashboard?section=reservations", CreatedAt = now };

        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            reservation.ReservationAt = command.NewReservationAt; reservation.UpdatedAt = now; reservations.Update(reservation);
            await changes.AddAsync(change); await notifications.AddAsync(notification); await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);
        }
        catch (Exception)
        {
            await unitOfWork.RollbackAsync(CancellationToken.None);
            return Result<ReservationTimeChangeResponse>.Failure("ثبت تغییر زمان رزرو انجام نشد");
        }

        try
        {
            await push.SendAsync(notification.UserId, notification.Title, notification.Body,
                new Dictionary<string, string> { ["type"] = notification.Type, ["reservationId"] = reservation.Id.ToString(), ["route"] = notification.Route! }, cancellationToken);
        }
        catch (Exception ex) { logger.LogError(ex, "Push failed for reservation {ReservationId}; inbox notification remains stored", reservation.Id); }

        return Result<ReservationTimeChangeResponse>.Success(ToResponse(reservation, change, true), "زمان رزرو تغییر کرد و برای تایید مشاور ارسال شد");
    }

    private static ReservationTimeChangeResponse ToResponse(DentalDashboard.Domain.Models.Reservation reservation, ReservationTimeChange change, bool waiting) => new()
    { ReservationId = reservation.Id, ReservationAt = reservation.ReservationAt, IsWaitingForConsultantTimeConfirmation = waiting,
      SecretaryTimeChangeNote = change.Note, SecretaryChangedReservationAt = change.CreatedAt };
}
