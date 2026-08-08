using DentalDashboard.ApplicationService.Contract.Requests.Reservation.Commands;
using DentalDashboard.ApplicationService.Contract.Responses.ReservationResponse;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Models;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using DentalDashboard.Framwork.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.Reservation;

public class ConfirmSecretaryTimeChangeCommandHandler : ICommandHandler<ConfirmSecretaryTimeChangeCommand, ReservationTimeChangeResponse>
{
    private readonly IReservationRepository reservations;
    private readonly IBaseRepository<long, ReservationTimeChange> changes;
    public ConfirmSecretaryTimeChangeCommandHandler(IReservationRepository reservations, IBaseRepository<long, ReservationTimeChange> changes)
    { this.reservations = reservations; this.changes = changes; }

    public async Task<Result<ReservationTimeChangeResponse>> HandleAsync(ConfirmSecretaryTimeChangeCommand command, CancellationToken cancellationToken = default)
    {
        var reservation = await reservations.GetAll().Include(x => x.ConsultantProfile)
            .SingleOrDefaultAsync(x => x.Id == command.ReservationId, cancellationToken);
        if (reservation == null || reservation.IsDeleted || reservation.IsCanceled)
            return Result<ReservationTimeChangeResponse>.Failure("رزرو فعال یافت نشد");
        if (reservation.ConsultantProfileId != command.ConsultantProfileId || reservation.ConsultantProfile.UserId != command.AuthenticatedUserId)
            return Result<ReservationTimeChangeResponse>.Failure("این رزرو متعلق به مشاور واردشده نیست");

        var change = await changes.GetAll().Where(x => x.ReservationId == reservation.Id)
            .OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync(cancellationToken);
        if (change == null)
            return Result<ReservationTimeChangeResponse>.Failure("درخواست تغییر زمان برای این رزرو یافت نشد");
        if (change.Status == ReservationTimeChangeStatus.Confirmed)
            return Result<ReservationTimeChangeResponse>.Success(Response(reservation, change), "زمان جدید رزرو قبلاً تایید شده است");
        if (change.Status != ReservationTimeChangeStatus.PendingConsultantConfirmation)
            return Result<ReservationTimeChangeResponse>.Failure("درخواست تغییر زمان در وضعیت قابل تایید نیست");

        change.Status = ReservationTimeChangeStatus.Confirmed; change.ConfirmedAt = DateTime.UtcNow;
        change.ConfirmedByConsultantProfileId = command.ConsultantProfileId; change.UpdatedAt = change.ConfirmedAt;
        changes.Update(change); await changes.SaveChange(cancellationToken);
        return Result<ReservationTimeChangeResponse>.Success(Response(reservation, change), "زمان جدید رزرو تایید شد");
    }

    private static ReservationTimeChangeResponse Response(DentalDashboard.Domain.Models.Reservation reservation, ReservationTimeChange change) => new()
    { ReservationId = reservation.Id, ReservationAt = reservation.ReservationAt, IsWaitingForConsultantTimeConfirmation = false,
      SecretaryTimeChangeNote = change.Note, SecretaryChangedReservationAt = change.CreatedAt };
}
