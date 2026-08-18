using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;

namespace DentalDashboard.ApplicationService.Contract.Requests.Reservation.Commands
{
    public class UpdateSecretaryAnnouncementCommand : ICommand
    {
        public long ReservationId { get; set; }
        public Guid SecretaryUserId { get; set; }
        public string? SecretaryAnnouncement { get; set; }
    }
}
