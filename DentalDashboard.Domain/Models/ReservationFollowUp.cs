using DentalDashboard.Domain.Enums;

namespace DentalDashboard.Domain.Models;

public class ReservationFollowUp : BaseAuditableEntity<long>
{
    public long ReservationId { get; set; }
    public Reservation Reservation { get; set; } = default!;
    public DateTime ScheduledAt { get; set; }
    public DateTime? ReminderAt { get; set; }
    public FollowUpStatus Status { get; set; } = FollowUpStatus.Pending;
    public FollowUpPriority Priority { get; set; } = FollowUpPriority.Normal;
    public string Reason { get; set; } = string.Empty;
    public Guid? AssignedSecretaryUserId { get; set; }
    public DateTime? CompletedAt { get; set; }
}
