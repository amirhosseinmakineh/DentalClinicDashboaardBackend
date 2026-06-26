using DentalDashboard.ApplicationService.Contract.Responses.ReservationResponse;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;

namespace DentalDashboard.ApplicationService.Contract.Requests.Reservation.Commands
{
    public class CreateReservationCommand : ICommand<CreateReservationResponse>
    {
        public long LeadAssignmentId { get; set; }
        public long ConsultantProfileId { get; set; }
        public DateTime ReservationAt { get; set; }
        public string PatientCity { get; set; } = default!;
        public int AttendanceProbabilityPercent { get; set; }
        public string AttendancePrediction { get; set; } = default!;
        public string? Description { get; set; }
    }
}
