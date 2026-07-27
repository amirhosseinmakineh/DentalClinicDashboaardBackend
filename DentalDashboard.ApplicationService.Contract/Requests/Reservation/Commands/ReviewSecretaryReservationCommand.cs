using DentalDashboard.ApplicationService.Contract.Responses.ReservationResponse;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;

namespace DentalDashboard.ApplicationService.Contract.Requests.Reservation.Commands;

public class ReviewSecretaryReservationCommand : ICommand<SecretaryReservationReviewResponse>
{
    public long ReservationId { get; set; }
    public Guid SecretaryUserId { get; set; }
    public DateTime? NewReservationAt { get; set; }
    public string? Note { get; set; }
}
