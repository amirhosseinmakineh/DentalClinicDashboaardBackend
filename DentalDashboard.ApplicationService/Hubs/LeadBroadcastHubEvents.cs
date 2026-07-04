namespace DentalDashboard.ApplicationService.Hubs;

public static class LeadBroadcastHubEvents
{
    public const string LeadBroadcastStarted = "LeadBroadcastStarted";
    public const string LeadClaimed = "LeadClaimed";
    public const string LeadBroadcastExpired = "LeadBroadcastExpired";
}

public sealed class LeadBroadcastStartedMessage
{
    public long LeadAssignmentId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? BroadcastStartedAt { get; init; }
}

public sealed class LeadClaimedMessage
{
    public long LeadAssignmentId { get; init; }
    public long ClaimedByConsultantProfileId { get; init; }
}

public sealed class LeadBroadcastExpiredMessage
{
    public long LeadAssignmentId { get; init; }
}
