namespace DentalDashboard.Domain.Strategies;

public sealed record SellerConsultantPolicy(
    int NewLeadDailyLimit,
    int BurnedLeadDailyLimit,
    int EvaluationDays,
    int GoldConfirmedPatientThreshold)
{
    public static SellerConsultantPolicy Default { get; } = new(10, 30, 10, 3);
}
