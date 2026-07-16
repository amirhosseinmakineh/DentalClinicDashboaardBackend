using DentalDashboard.ApplicationService.Contract.Responses.ReservationResponse;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;

namespace DentalDashboard.ApplicationService.Contract.Requests.Reservation.Commands
{
    public class UpdateReservationCommand : ICommand<ReservationItemResponse>
    {
        public long ReservationId { get; set; }
        public long ConsultantProfileId { get; set; }
        public DateTime ReservationAt { get; set; }
        public string? Description { get; set; }
        public string? PatientCity { get; set; }
        public string? PatientRegion { get; set; }
        public int? AttendanceProbabilityPercent { get; set; }
        public string? AttendancePrediction { get; set; }
        public string? SecondaryPhoneNumber { get; set; }
    }
}
