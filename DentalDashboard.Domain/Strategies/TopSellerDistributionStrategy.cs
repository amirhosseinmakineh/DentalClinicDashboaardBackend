using DentalDashboard.Domain.Enums;

namespace DentalDashboard.Domain.Strategies;

public sealed class TopSellerDistributionStrategy
{
    private readonly TopSellerPolicy policy;
    private readonly ConsultantDistributionPolicy distributionPolicy;

    public TopSellerDistributionStrategy(TopSellerPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        if (policy.RealTimeDailyLimit <= 0 || policy.BurnedDailyLimit != 0 ||
            policy.EvaluationDays <= 0 || policy.MinimumPatientsToRemain <= 0 ||
            policy.RewardLevel1Threshold < policy.MinimumPatientsToRemain ||
            policy.RewardLevel2Threshold <= policy.RewardLevel1Threshold)
            throw new ArgumentOutOfRangeException(nameof(policy));
        this.policy = policy;
        distributionPolicy = ConsultantDistributionPolicyResolver.Resolve(ConsultantLevel.TopSeller);
    }

    public TopSellerDecision Decide(TopSellerContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var remaining = Math.Max(0,
            distributionPolicy.RealTimeDailyLimit - Math.Max(0, context.AssignedRealTimeToday));
        var ready = context.CurrentTime >= context.TopSellerStartedAt.Date.AddDays(policy.EvaluationDays);
        var remains = ready && context.SuccessfulPatients >= policy.MinimumPatientsToRemain;
        var reward = !remains ? TopSellerRewardLevel.None
            : context.SuccessfulPatients >= policy.RewardLevel2Threshold ? TopSellerRewardLevel.Level2
            : context.SuccessfulPatients >= policy.RewardLevel1Threshold ? TopSellerRewardLevel.Level1
            : TopSellerRewardLevel.None;

        return new TopSellerDecision(
            remaining > 0 && context.IsActive && context.IsAvailable && context.IsOnline,
            remaining,
            distributionPolicy.AllowsBurned,
            ready,
            remains,
            ready && !remains,
            reward);
    }
}
