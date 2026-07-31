using DentalDashboard.Domain.Enums;

namespace DentalDashboard.Domain.Models;

public sealed class ReservationContactLog : BaseAuditableEntity<long>
{
    public long ReservationId { get; set; }
    public Reservation Reservation { get; set; } = default!;
    public ReservationContactResult Result { get; set; }
    public string? Note { get; set; }
    public Guid CreatedByUserId { get; set; }
}
