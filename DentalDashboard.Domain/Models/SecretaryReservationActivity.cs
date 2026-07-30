namespace DentalDashboard.Domain.Models;

public class SecretaryReservationActivity : BaseAuditableEntity<long>
{
    public long ReservationId { get; set; }
    public Reservation Reservation { get; set; } = default!;
    public Guid ActorUserId { get; set; }
    public User ActorUser { get; set; } = default!;
    public string ActivityType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
