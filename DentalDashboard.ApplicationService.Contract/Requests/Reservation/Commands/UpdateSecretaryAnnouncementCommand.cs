using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Domain.Enums;
using System.Text.Json.Serialization;
using DentalDashboard.ApplicationService.Contract.Serialization;

namespace DentalDashboard.ApplicationService.Contract.Requests.Reservation.Commands
{
    public class UpdateSecretaryAnnouncementCommand : ICommand
    {
        public long ReservationId { get; set; }
        [JsonConverter(typeof(SecretaryAnnouncementStatusJsonConverter))]
        public SecretaryAnnouncementStatus Status { get; set; }
        public string? Description { get; set; }

        [JsonIgnore]
        public Guid SecretaryUserId { get; set; }
    }
}
