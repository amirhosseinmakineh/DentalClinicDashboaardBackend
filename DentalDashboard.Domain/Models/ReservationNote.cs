namespace DentalDashboard.Domain.Models;

public sealed class ReservationNote : BaseAuditableEntity<long>
{
    public long ReservationId { get; set; }
    public Reservation Reservation { get; set; } = default!;
    public string Note { get; set; } = string.Empty;
    public Guid CreatedByUserId { get; set; }
}
