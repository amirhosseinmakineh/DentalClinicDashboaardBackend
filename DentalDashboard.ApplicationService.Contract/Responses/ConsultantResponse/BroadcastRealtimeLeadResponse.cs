namespace DentalDashboard.ApplicationService.Contract.Responses.ConsultantResponse;

public sealed class BroadcastRealtimeLeadItemResponse
{
    public long LeadAssignmentId { get; init; }
    public string? UserName { get; init; }
    public string? PhoneNumber { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed class BroadcastRealtimeLeadsResponse
{
    public bool CanReceive { get; init; }
    public string? BlockReason { get; init; }
    public IReadOnlyList<BroadcastRealtimeLeadItemResponse> Leads { get; init; } =
        Array.Empty<BroadcastRealtimeLeadItemResponse>();
}
