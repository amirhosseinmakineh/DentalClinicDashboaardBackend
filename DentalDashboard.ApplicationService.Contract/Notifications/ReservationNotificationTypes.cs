namespace DentalDashboard.ApplicationService.Contract.Notifications;

public static class ReservationNotificationTypes
{
    public const string ReservationSecretaryNoAnswer = nameof(ReservationSecretaryNoAnswer);
    public const string ReservationSecretaryConfirmed = nameof(ReservationSecretaryConfirmed);
    public const string ReservationSecretaryCancelled = nameof(ReservationSecretaryCancelled);
}
