namespace DentalDashboard.Domain.Strategies;

public sealed record TopSellerPolicy(
    int RealTimeDailyLimit,
    int BurnedDailyLimit,
    int EvaluationDays,
    int MinimumPatientsToRemain,
    int RewardLevel1Threshold,
    int RewardLevel2Threshold)
{
    public static TopSellerPolicy Default { get; } = new(
        ConsultantDistributionPolicyResolver.Resolve(DentalDashboard.Domain.Enums.ConsultantLevel.TopSeller).RealTimeDailyLimit,
        ConsultantDistributionPolicyResolver.Resolve(DentalDashboard.Domain.Enums.ConsultantLevel.TopSeller).BurnedDailyLimit,
        7, 4, 7, 10);
}
