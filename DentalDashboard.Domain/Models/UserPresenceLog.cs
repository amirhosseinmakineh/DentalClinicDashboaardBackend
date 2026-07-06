using DentalDashboard.Domain.Enums;

namespace DentalDashboard.Domain.Models;

public class UserPresenceLog : BaseAuditableEntity<long>
{
    public Guid UserId { get; set; }

    public UserPresenceEventType EventType { get; set; }

    public DateTime OccurredAt { get; set; }

    public string? Description { get; set; }

    public User User { get; set; } = default!;
}
