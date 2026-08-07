namespace DentalDashboard.Domain.Strategies;

public sealed record SellerConsultantContext
{
    public required DateTime SellerStartedAt { get; init; }
    public required DateTime CurrentTime { get; init; }
    public int AssignedNewLeadToday { get; init; }
    public int AssignedBurnedLeadToday { get; init; }
    public int ConfirmedPatientCount { get; init; }
    public bool IsActive { get; init; }
    public bool IsAvailable { get; init; }
    public bool IsOnline { get; init; }
}
