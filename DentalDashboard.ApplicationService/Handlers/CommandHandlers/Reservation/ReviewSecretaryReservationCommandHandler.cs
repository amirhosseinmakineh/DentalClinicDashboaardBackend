using DentalDashboard.ApplicationService.Contract.Requests.Reservation.Commands;
using DentalDashboard.ApplicationService.Contract.Responses.ReservationResponse;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.Reservation;

public class ReviewSecretaryReservationCommandHandler :
    ICommandHandler<ReviewSecretaryReservationCommand, SecretaryReservationReviewResponse>
{
    private const int MaxReservationsPerConsultantAtSameTime = 10;
    private const int MaxNoteLength = 1000;
    private readonly IReservationRepository reservationRepository;

    public ReviewSecretaryReservationCommandHandler(IReservationRepository reservationRepository)
    {
        this.reservationRepository = reservationRepository;
    }

    public async Task<Result<SecretaryReservationReviewResponse>> HandleAsync(
        ReviewSecretaryReservationCommand command,
        CancellationToken cancellationToken = default)
    {
        var reservation = await reservationRepository.GetByIdAsync(command.ReservationId);
        if (reservation == null || reservation.IsDeleted || reservation.IsCanceled)
            return Result<SecretaryReservationReviewResponse>.Failure("رزرو فعال یافت نشد");

        if (command.SecretaryUserId == Guid.Empty)
            return Result<SecretaryReservationReviewResponse>.Failure("شناسه منشی الزامی است");

        if (reservation.SecretaryReservationReviewStatus != SecretaryReservationReviewStatus.Pending)
            return Result<SecretaryReservationReviewResponse>.Failure("این رزرو قبلا توسط منشی بررسی شده است");

        var note = string.IsNullOrWhiteSpace(command.Note) ? null : command.Note.Trim();
        if (note?.Length > MaxNoteLength)
            return Result<SecretaryReservationReviewResponse>.Failure("توضیحات منشی نمی‌تواند بیشتر از ۱۰۰۰ کاراکتر باشد");

        var targetReservationAt = command.NewReservationAt ?? reservation.ReservationAt;
        if (targetReservationAt <= DateTime.Now)
            return Result<SecretaryReservationReviewResponse>.Failure("زمان رزرو باید در آینده باشد");

        var reservationTimeChanged = targetReservationAt != reservation.ReservationAt;
        if (reservationTimeChanged)
        {
            var sameTimeCount = await reservationRepository.CountActiveReservationsAtExcludingAsync(
                reservation.ConsultantProfileId,
                targetReservationAt,
                reservation.Id);
            if (sameTimeCount >= MaxReservationsPerConsultantAtSameTime)
                return Result<SecretaryReservationReviewResponse>.Failure("ظرفیت این بازه زمانی برای مشاور تکمیل است");
        }

        var reviewedAt = DateTime.UtcNow;
        reservation.ReservationAt = targetReservationAt;
        reservation.SecretaryReservationReviewStatus = reservationTimeChanged
            ? SecretaryReservationReviewStatus.Rescheduled
            : SecretaryReservationReviewStatus.Approved;
        reservation.SecretaryReservationReviewedAt = reviewedAt;
        reservation.SecretaryReservationReviewerUserId = command.SecretaryUserId;
        reservation.SecretaryReservationReviewNote = note;
        reservation.UpdatedAt = reviewedAt;

        reservationRepository.Update(reservation);
        await reservationRepository.SaveChange();

        return Result<SecretaryReservationReviewResponse>.Success(
            new SecretaryReservationReviewResponse
            {
                ReservationId = reservation.Id,
                ReservationAt = reservation.ReservationAt,
                ReviewStatus = reservation.SecretaryReservationReviewStatus,
                ReviewedAt = reviewedAt,
                SecretaryUserId = command.SecretaryUserId,
                Note = note
            },
            reservationTimeChanged
                ? "زمان رزرو با موفقیت توسط منشی تغییر کرد"
                : "رزرو با موفقیت توسط منشی تایید شد");
    }
}
