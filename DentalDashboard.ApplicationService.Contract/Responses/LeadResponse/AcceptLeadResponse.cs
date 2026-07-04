namespace DentalDashboard.ApplicationService.Contract.Responses.LeadResponse;

public class AcceptLeadResponse
{
    public long LeadAssignmentId { get; set; }
    public long ConsultantProfileId { get; set; }
    public int LeadAssignmentState { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
}
