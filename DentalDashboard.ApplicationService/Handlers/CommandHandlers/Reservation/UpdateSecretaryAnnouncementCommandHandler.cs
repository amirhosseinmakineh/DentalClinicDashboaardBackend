using DentalDashboard.ApplicationService.Contract.Requests.Reservation.Commands;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.Reservation
{
    public class UpdateSecretaryAnnouncementCommandHandler : ICommandHandler<UpdateSecretaryAnnouncementCommand>
    {
        private const int MaximumAnnouncementLength = 1000;
        private readonly IReservationRepository reservationRepository;

        public UpdateSecretaryAnnouncementCommandHandler(IReservationRepository reservationRepository)
        {
            this.reservationRepository = reservationRepository;
        }

        public async Task<Result> HandleAsync(
            UpdateSecretaryAnnouncementCommand command,
            CancellationToken cancellationToken = default)
        {
            var reservation = await reservationRepository.GetByIdAsync(command.ReservationId);
            if (reservation == null || reservation.IsDeleted)
                return Result.Failure("رزرو یافت نشد");

            if (reservation.IsCanceled)
                return Result.Failure("امکان ثبت اعلام منشی برای رزرو لغوشده وجود ندارد");

            var announcement = command.SecretaryAnnouncement?.Trim();
            if (announcement?.Length > MaximumAnnouncementLength)
                return Result.Failure("اعلام منشی نباید بیشتر از ۱۰۰۰ کاراکتر باشد");

            reservation.SecretaryAnnouncement = string.IsNullOrWhiteSpace(announcement) ? null : announcement;
            reservation.SecretaryAnnouncementUserId = command.SecretaryUserId;
            reservation.SecretaryAnnouncementUpdatedAt = DateTime.UtcNow;
            reservation.UpdatedAt = DateTime.UtcNow;

            reservationRepository.Update(reservation);
            await reservationRepository.SaveChange();

            return Result.Success("اعلام منشی با موفقیت ثبت شد");
        }
    }
}
