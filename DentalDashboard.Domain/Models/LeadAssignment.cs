using DentalDashboard.Domain.Enums;

namespace DentalDashboard.Domain.Models;

public class LeadAssignment : BaseAuditableEntity<long>
{
    public string UserName { get; set; } = default!;
    public string PhoneNumber { get; set; } = default!;
    public LeadAssignmentState LeadAssignmentState { get; set; }
    public long? ConsultantProfileId { get; set; }
    public ConsultantProfile? ConsultantProfile { get; set; }
    public DateTime? AssignedAt { get; set; }
    public LeadAssignmentType AssignmentType { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CallDeadlineAt { get; set; }
    public bool RequiresThreeMinuteCall { get; set; }
    public bool NotificationSent { get; set; }
    public string? ReportDescription { get; set; }
    public DateTime? ReportSubmittedAt { get; set; }
    public DateTime? ContactedAt { get; set; }
    public LeadCallResult? CallResult { get; set; }
    public bool SmsSent { get; set; }
    public string? PatientCity { get; set; }
    public string? PatientRegion { get; set; }
    public string? BusinessName { get; set; }
    public int? AttendanceProbabilityPercent { get; set; }

}