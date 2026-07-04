namespace DentalDashboard.ApplicationService.Contract.Responses.LeadResponse;

public class SeedTestBroadcastLeadsResponse
{
    public List<SeedTestBroadcastLeadItem> Leads { get; set; } = [];
    public int OnlineTestConsultantCount { get; set; }
}

public class SeedTestBroadcastLeadItem
{
    public long LeadAssignmentId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
}
