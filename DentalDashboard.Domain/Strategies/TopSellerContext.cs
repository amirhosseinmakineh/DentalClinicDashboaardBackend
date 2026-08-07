namespace DentalDashboard.Domain.Strategies;

public sealed record TopSellerContext
{
    public required DateTime TopSellerStartedAt { get; init; }
    public required DateTime CurrentTime { get; init; }
    public int AssignedRealTimeToday { get; init; }
    public int SuccessfulPatients { get; init; }
    public bool IsActive { get; init; }
    public bool IsAvailable { get; init; }
    public bool IsOnline { get; init; }
}
