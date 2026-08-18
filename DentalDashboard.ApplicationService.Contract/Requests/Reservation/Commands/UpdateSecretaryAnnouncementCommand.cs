using DentalDashboard.Domain.Enums;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using System.Text.Json.Serialization;

namespace DentalDashboard.ApplicationService.Contract.Requests.Reservation.Commands;

public class UpdateSecretaryAnnouncementCommand : ICommand
{
    public long ReservationId { get; set; }
    public SecretaryAnnouncementStatus Status { get; set; }
    public string? Description { get; set; }
    [JsonIgnore]
    public Guid SecretaryUserId { get; set; }
}
