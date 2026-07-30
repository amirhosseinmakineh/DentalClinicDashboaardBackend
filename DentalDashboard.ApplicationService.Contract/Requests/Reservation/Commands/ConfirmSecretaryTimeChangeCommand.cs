using System.Text.Json.Serialization;
using DentalDashboard.ApplicationService.Contract.Responses.ReservationResponse;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;

namespace DentalDashboard.ApplicationService.Contract.Requests.Reservation.Commands;

public class ConfirmSecretaryTimeChangeCommand : ICommand<ReservationTimeChangeResponse>
{
    public long ReservationId { get; set; }
    public long ConsultantProfileId { get; set; }
    [JsonIgnore] public Guid AuthenticatedUserId { get; set; }
}
