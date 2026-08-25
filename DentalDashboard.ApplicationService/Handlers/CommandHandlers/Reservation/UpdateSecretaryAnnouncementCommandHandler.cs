using DentalDashboard.ApplicationService.Contract.Requests.Reservation.Commands;
using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.ApplicationService.Contract.Notifications;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.Reservation
{
    public class UpdateSecretaryAnnouncementCommandHandler : ICommandHandler<UpdateSecretaryAnnouncementCommand>
    {
        private const int MaximumAnnouncementLength = 1000;
        private readonly IReservationRepository reservationRepository;
        private readonly IPushNotificationService pushNotificationService;

        public UpdateSecretaryAnnouncementCommandHandler(
            IReservationRepository reservationRepository,
            IPushNotificationService pushNotificationService)
        {
            this.reservationRepository = reservationRepository;
            this.pushNotificationService = pushNotificationService;
        }

        public async Task<Result> HandleAsync(
            UpdateSecretaryAnnouncementCommand command,
            CancellationToken cancellationToken = default)
        {
            if (command.SecretaryUserId == Guid.Empty)
                return Result.Failure("کاربر منشی معتبر نیست");

            if (!Enum.IsDefined(command.Status))
                return Result.Failure("وضعیت اعلام منشی معتبر نیست");

            var reservation = await reservationRepository.GetAll()
                .Include(x => x.LeadAssignment)
                .Include(x => x.ConsultantProfile)
                .FirstOrDefaultAsync(x => x.Id == command.ReservationId, cancellationToken);
            if (reservation == null || reservation.IsDeleted)
                return Result.Failure("رزرو یافت نشد");

            if (reservation.IsCanceled)
                return Result.Failure("امکان ثبت اعلام منشی برای رزرو لغوشده وجود ندارد");

            var announcement = command.Description?.Trim();
            if (announcement?.Length > MaximumAnnouncementLength)
                return Result.Failure("اعلام منشی نباید بیشتر از ۱۰۰۰ کاراکتر باشد");

            reservation.SecretaryAnnouncement = string.IsNullOrWhiteSpace(announcement) ? null : announcement;
            reservation.SecretaryAnnouncementStatus = command.Status;
            reservation.SecretaryAnnouncementUserId = command.SecretaryUserId;
            reservation.SecretaryAnnouncementUpdatedAt = DateTime.UtcNow;
            reservation.UpdatedAt = DateTime.UtcNow;
            reservation.Description = command.Description;

            reservationRepository.Update(reservation);
            await reservationRepository.SaveChange();

            await SendConsultantNotificationAsync(reservation, command.Status, cancellationToken);

            return Result.Success("اعلام منشی با موفقیت ثبت شد");
        }

        private async Task SendConsultantNotificationAsync(
            DentalDashboard.Domain.Models.Reservation reservation,
            SecretaryAnnouncementStatus status,
            CancellationToken cancellationToken)
        {
            var notification = status switch
            {
                SecretaryAnnouncementStatus.NoAnswer => (
                    ReservationNotificationTypes.ReservationSecretaryNoAnswer,
                    $"بیمار {reservation.LeadAssignment.UserName} پاسخگوی تماس منشی نبود."),
                SecretaryAnnouncementStatus.Confirmed => (
                    ReservationNotificationTypes.ReservationSecretaryConfirmed,
                    "بیمار رزرو خود را تایید کرد."),
                SecretaryAnnouncementStatus.CancelledByPatient => (
                    ReservationNotificationTypes.ReservationSecretaryCancelled,
                    "بیمار رزرو خود را لغو کرد."),
                _ => default
            };

            if (notification == default)
                return;

            await pushNotificationService.SendAsync(
                reservation.ConsultantProfile.UserId,
                "اعلام منشی رزرو",
                notification.Item2,
                new Dictionary<string, string>
                {
                    ["type"] = notification.Item1,
                    ["reservationId"] = reservation.Id.ToString(),
                    ["patientName"] = reservation.LeadAssignment.UserName ?? string.Empty,
                    ["reservationDate"] = reservation.ReservationAt.ToString("yyyy-MM-dd"),
                    ["message"] = notification.Item2
                },
                cancellationToken);
        }
    }
}
