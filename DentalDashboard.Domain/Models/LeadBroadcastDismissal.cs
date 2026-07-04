namespace DentalDashboard.Domain.Models;

public class LeadBroadcastDismissal : BaseAuditableEntity<long>
{
    public long LeadAssignmentId { get; set; }
    public LeadAssignment? LeadAssignment { get; set; }
    public long ConsultantProfileId { get; set; }
    public ConsultantProfile? ConsultantProfile { get; set; }
}
