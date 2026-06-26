using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;

namespace DentalDashboard.ApplicationService.Contract.Requests.Reservation.Commands
{
    public class ConfirmReservationAttendanceCommand : ICommand
    {
        public long ReservationId { get; set; }
        public long ConsultantProfileId { get; set; }
        public bool PatientAttended { get; set; }
        public string? Note { get; set; }
    }
}
