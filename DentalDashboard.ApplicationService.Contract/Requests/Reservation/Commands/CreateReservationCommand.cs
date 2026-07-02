using DentalDashboard.ApplicationService.Contract.Responses.ReservationResponse;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;

namespace DentalDashboard.ApplicationService.Contract.Requests.Reservation.Commands
{
    public class CreateReservationCommand : ICommand<CreateReservationResponse>
    {
        public long LeadAssignmentId { get; set; }
        public long ConsultantProfileId { get; set; }
        public DateTime ReservationAt { get; set; }
        public string? Description { get; set; }
    }
}
