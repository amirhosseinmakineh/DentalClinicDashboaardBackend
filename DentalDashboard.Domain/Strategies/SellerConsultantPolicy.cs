namespace DentalDashboard.Domain.Strategies;

public sealed record SellerConsultantPolicy(
    int NewLeadDailyLimit,
    int BurnedLeadDailyLimit,
    int EvaluationDays,
    int GoldConfirmedPatientThreshold)
{
    public static SellerConsultantPolicy Default { get; } = new(
        ConsultantDistributionPolicyResolver.Resolve(DentalDashboard.Domain.Enums.ConsultantLevel.Seller).RealTimeDailyLimit,
        ConsultantDistributionPolicyResolver.Resolve(DentalDashboard.Domain.Enums.ConsultantLevel.Seller).BurnedDailyLimit,
        10, 3);
}
