namespace DentalDashboard.Domain.Enums;

public enum ReservationRequestStatus
{
    PendingSecretaryReview = 1,
    Confirmed = 2,
    Rescheduled = 3,
    Rejected = 4,
    Canceled = 5
}
