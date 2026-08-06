namespace DentalDashboard.Domain.Strategies;

public sealed record TestConsultantPolicy(
    int DailyLeadLimit,
    int DistributionDays,
    int TotalTestDays,
    int RequiredConfirmedPatients)
{
    public static TestConsultantPolicy Default { get; } = new(20, 5, 10, 1);
}
