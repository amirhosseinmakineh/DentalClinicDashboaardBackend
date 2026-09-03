using DentalDashboard.Domain.Enums;

namespace DentalDashboard.Domain.Models;

public class LeadAssignmentHistory : BaseAuditableEntity<long>
{
    public long LeadAssignmentId { get; set; }
    public LeadAssignment LeadAssignment { get; set; } = default!;
    public long? PreviousConsultantProfileId { get; set; }
    public ConsultantProfile? PreviousConsultantProfile { get; set; }
    public long NewConsultantProfileId { get; set; }
    public ConsultantProfile NewConsultantProfile { get; set; } = default!;
    public LeadAssignmentSourceType AssignmentSourceType { get; set; }
    public LeadAssignmentState PreviousState { get; set; }
    public DateTime? PreviousAssignedAt { get; set; }
    public string? PreviousReportDescription { get; set; }
    public DateTime? PreviousReportSubmittedAt { get; set; }
    public LeadCallResult? PreviousCallResult { get; set; }
    public DateTime AssignedAt { get; set; }
}
