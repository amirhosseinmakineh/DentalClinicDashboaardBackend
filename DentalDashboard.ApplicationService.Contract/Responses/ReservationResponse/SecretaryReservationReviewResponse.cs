using DentalDashboard.Domain.Enums;

namespace DentalDashboard.ApplicationService.Contract.Responses.ReservationResponse;

public class SecretaryReservationReviewResponse
{
    public long ReservationId { get; set; }
    public DateTime ReservationAt { get; set; }
    public SecretaryReservationReviewStatus ReviewStatus { get; set; }
    public DateTime ReviewedAt { get; set; }
    public Guid SecretaryUserId { get; set; }
    public string? Note { get; set; }
}
