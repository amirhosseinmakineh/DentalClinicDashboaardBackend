namespace DentalDashboard.Domain.Models;

public class Reservation : BaseAuditableEntity<long>
{
    public long LeadAssignmentId { get; set; }
    public LeadAssignment LeadAssignment { get; set; } = default!;
    public long ConsultantProfileId { get; set; }
    public ConsultantProfile ConsultantProfile { get; set; } = default!;
    public Guid? PatientUserId { get; set; }
    public User? PatientUser { get; set; }
    public DateTime ReservationAt { get; set; }
    public string? Description { get; set; }
    public bool IsCanceled { get; set; }
    public DateTime? CanceledAt { get; set; }
}