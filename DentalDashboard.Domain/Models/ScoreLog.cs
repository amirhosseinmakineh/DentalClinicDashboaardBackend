using DentalDashboard.Domain.Models;

public class ScoreLog : BaseAuditableEntity<long>
{
    public long ConsultantProfileId { get; set; }

    public ScoreSource Source { get; set; }

    public ScoreReason Reason { get; set; }

    public int ScoreValue { get; set; }

    public string? Description { get; set; }

    public long? LeadAssignmentId { get; set; }

    public Guid? CreatedByUserId { get; set; }
    public Guid UserId { get; set; }

    public ConsultantProfile ConsultantProfile { get; set; } = default!;
    public User User { get; set; }
    public LeadAssignment LeadAssignment { get; set; }
}