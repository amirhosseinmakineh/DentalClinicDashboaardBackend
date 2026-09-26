namespace DentalDashboard.ApplicationService.Contract.Responses.Secretary;

public sealed class AfterSalesPatientOptionResponse
{
    public long LeadAssignmentId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public long? ConsultantProfileId { get; init; }
    public string? ConsultantFullName { get; init; }
    public string? ConsultantPhoneNumber { get; init; }
}
