namespace DentalDashboard.Domain.Models;

public class UserNotification : BaseAuditableEntity<long>
{
    public Guid UserId { get; set; }
    public string Type { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Body { get; set; } = default!;
    public long? ReservationId { get; set; }
    public string? Route { get; set; }
    public DateTime? ReadAt { get; set; }
}
