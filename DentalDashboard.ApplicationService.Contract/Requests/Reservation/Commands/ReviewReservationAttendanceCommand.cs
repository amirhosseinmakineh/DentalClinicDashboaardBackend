using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;

namespace DentalDashboard.ApplicationService.Contract.Requests.Reservation.Commands
{
    public class ReviewReservationAttendanceCommand : ICommand
    {
        public long ReservationId { get; set; }
        public Guid SecretaryUserId { get; set; }
        public bool Approved { get; set; }
        public string? Note { get; set; }
    }
}
