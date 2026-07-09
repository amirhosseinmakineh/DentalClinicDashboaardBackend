namespace DentalDashboard.ApplicationService.Contract.Responses.LeadResponse;

public enum PickupLeadStatus
{
    Success = 1,
    AlreadyTaken = 2,
    DailyLimitReached = 3
}

public sealed class PickupLeadResult
{
    public PickupLeadStatus Status { get; init; }
    public long? LeadAssignmentId { get; init; }
    public long? ConsultantProfileId { get; init; }
    public DateTime? CallDeadlineAt { get; init; }
}
