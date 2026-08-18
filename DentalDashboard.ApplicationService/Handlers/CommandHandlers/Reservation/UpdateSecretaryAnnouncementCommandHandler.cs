using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.ApplicationService.Contract.Requests.Reservation.Commands;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.Reservation;

public class UpdateSecretaryAnnouncementCommandHandler : ICommandHandler<UpdateSecretaryAnnouncementCommand>
{
    private readonly IReservationRepository reservationRepository;
    private readonly IPushNotificationService pushNotificationService;

    public UpdateSecretaryAnnouncementCommandHandler(
        IReservationRepository reservationRepository,
        IPushNotificationService pushNotificationService)
    {
        this.reservationRepository = reservationRepository;
        this.pushNotificationService = pushNotificationService;
    }

    public async Task<Result> HandleAsync(UpdateSecretaryAnnouncementCommand command, CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(command.Status))
            return Result.Failure("وضعیت اعلام منشی معتبر نیست");

        var description = string.IsNullOrWhiteSpace(command.Description) ? null : command.Description.Trim();
        if (description?.Length > 1000)
            return Result.Failure("توضیحات نمی‌تواند بیشتر از ۱۰۰۰ کاراکتر باشد");

        var reservation = await reservationRepository.GetAll()
            .Include(x => x.ConsultantProfile)
            .Include(x => x.LeadAssignment)
            .FirstOrDefaultAsync(x => x.Id == command.ReservationId, cancellationToken);

        if (reservation == null || reservation.IsDeleted)
            return Result.Failure("رزرو یافت نشد");
        if (reservation.IsCanceled)
            return Result.Failure("رزرو لغوشده قابل به‌روزرسانی نیست");

        var updatedAt = DateTime.UtcNow;
        reservation.SecretaryAnnouncementStatus = command.Status;
        reservation.SecretaryAnnouncement = description;
        reservation.SecretaryAnnouncementUpdatedAt = updatedAt;
        reservation.SecretaryAnnouncementUserId = command.SecretaryUserId;
        reservation.UpdatedAt = updatedAt;
        reservationRepository.Update(reservation);
        await reservationRepository.SaveChange();

        var notification = BuildNotification(command.Status, reservation.LeadAssignment.UserName);
        if (notification != null)
        {
            var data = new Dictionary<string, string>
            {
                ["type"] = notification.Value.Type,
                ["reservationId"] = reservation.Id.ToString(),
                ["patientName"] = reservation.LeadAssignment.UserName,
                ["message"] = notification.Value.Message
            };
            if (command.Status == SecretaryAnnouncementStatus.NoAnswer)
                data["reservationDate"] = reservation.ReservationAt.ToString("yyyy-MM-dd");

            await pushNotificationService.SendAsync(
                reservation.ConsultantProfile.UserId,
                "پیگیری رزرو توسط منشی",
                notification.Value.Message,
                data,
                cancellationToken);
        }

        return Result.Success("نتیجه تماس منشی ثبت شد");
    }

    private static (string Type, string Message)? BuildNotification(SecretaryAnnouncementStatus status, string patientName) => status switch
    {
        SecretaryAnnouncementStatus.NoAnswer => ("ReservationSecretaryNoAnswer", $"بیمار {patientName} پاسخگوی تماس منشی نبود."),
        SecretaryAnnouncementStatus.Confirmed => ("ReservationSecretaryConfirmed", "بیمار رزرو خود را تایید کرد."),
        SecretaryAnnouncementStatus.CancelledByPatient => ("ReservationSecretaryCancelled", "بیمار رزرو خود را لغو کرد."),
        _ => null
    };
}
