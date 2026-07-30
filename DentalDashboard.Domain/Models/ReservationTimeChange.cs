using DentalDashboard.Domain.Enums;

namespace DentalDashboard.Domain.Models;

public class ReservationTimeChange : BaseAuditableEntity<long>
{
    public long ReservationId { get; set; }
    public Reservation Reservation { get; set; } = default!;
    public DateTime PreviousReservationAt { get; set; }
    public DateTime NewReservationAt { get; set; }
    public Guid ChangedBySecretaryUserId { get; set; }
    public string? Note { get; set; }
    public ReservationTimeChangeStatus Status { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public long? ConfirmedByConsultantProfileId { get; set; }
}
