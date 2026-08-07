namespace DentalDashboard.Domain.Strategies;

public sealed record TestConsultantContext
{
    public required DateTime TestStartedAt { get; init; }
    public required DateTime CurrentTime { get; init; }
    public int AssignedTodayCount { get; init; }
    public int ConfirmedPatientCount { get; init; }
    public bool IsActive { get; init; }
    public bool IsAvailable { get; init; }
    public bool IsOnline { get; init; }
}
