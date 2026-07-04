namespace DentalDashboard.ApplicationService.Contract.Responses.LeadResponse;

public class BroadcastingLeadResponse
{
    public long LeadAssignmentId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? BroadcastStartedAt { get; set; }
    public int LeadAssignmentType { get; set; }
}
