namespace DentalDashboard.Domain.Models;

public sealed class ReservationNotificationOutbox : BaseAuditableEntity<long>
{
    public long ReservationId { get; set; }
    public long ActivityId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = "{}";
    public string IdempotencyKey { get; set; } = string.Empty;
    public DateTime? ProcessedAt { get; set; }
    public string? LastError { get; set; }
}
