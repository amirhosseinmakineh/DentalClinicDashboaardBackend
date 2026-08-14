using DentalDashboard.Domain.Enums;

namespace DentalDashboard.Domain.RolePolicies;

public sealed class ConsultantRolePolicyProvider : IConsultantRolePolicyProvider
{
    private static readonly IReadOnlyDictionary<ConsultantRole, ConsultantRolePolicy> Policies =
        new Dictionary<ConsultantRole, ConsultantRolePolicy>
        {
            [ConsultantRole.Test] = new(ConsultantRole.Test, TimeSpan.FromDays(10), 0, 20, TimeSpan.FromDays(5), 1, 1, int.MaxValue, int.MaxValue),
            [ConsultantRole.Seller] = new(ConsultantRole.Seller, TimeSpan.FromDays(10), 10, 30, null, 3, 1, int.MaxValue, int.MaxValue),
            [ConsultantRole.TopSeller] = new(ConsultantRole.TopSeller, TimeSpan.FromDays(7), 20, 0, null, int.MaxValue, 4, 7, 10)
        };

    public ConsultantRolePolicy Get(ConsultantRole role) =>
        Policies.TryGetValue(role, out var policy)
            ? policy
            : throw new ArgumentOutOfRangeException(nameof(role), role, "Consultant role policy is not configured.");

    public int GetDailyLimit(ConsultantRole role, LeadLimitType leadType, DateTime periodStartedAt, DateTime now)
    {
        var policy = Get(role);
        if (policy.LeadReceptionPeriod.HasValue && now >= periodStartedAt + policy.LeadReceptionPeriod.Value)
            return 0;

        return leadType == LeadLimitType.Realtime ? policy.RealtimeDailyLimit : policy.BurntDailyLimit;
    }

    public ConsultantRoleEvaluationDecision Evaluate(ConsultantRole role, int successfulPatientCount)
    {
        var policy = Get(role);
        return role switch
        {
            ConsultantRole.Test when successfulPatientCount >= policy.PromotionThreshold =>
                new(ConsultantRole.Seller, ConsultantEvaluationResult.PromotedToSeller, 0, false),
            ConsultantRole.Test =>
                new(ConsultantRole.Test, ConsultantEvaluationResult.Deactivated, 0, true),
            ConsultantRole.Seller when successfulPatientCount >= policy.PromotionThreshold =>
                new(ConsultantRole.TopSeller, ConsultantEvaluationResult.PromotedToTopSeller, 0, false),
            ConsultantRole.Seller when successfulPatientCount < policy.DemotionThreshold =>
                new(ConsultantRole.Test, ConsultantEvaluationResult.DemotedToTest, 0, false),
            ConsultantRole.Seller =>
                new(ConsultantRole.Seller, ConsultantEvaluationResult.RemainedSeller, 0, false),
            ConsultantRole.TopSeller when successfulPatientCount < policy.DemotionThreshold =>
                new(ConsultantRole.Seller, ConsultantEvaluationResult.DemotedToSeller, 0, false),
            ConsultantRole.TopSeller when successfulPatientCount >= policy.HigherRewardThreshold =>
                new(ConsultantRole.TopSeller, ConsultantEvaluationResult.TopSellerHigherReward, 2, false),
            ConsultantRole.TopSeller when successfulPatientCount >= policy.RewardThreshold =>
                new(ConsultantRole.TopSeller, ConsultantEvaluationResult.TopSellerReward, 1, false),
            _ => new(ConsultantRole.TopSeller, ConsultantEvaluationResult.RemainedTopSeller, 0, false)
        };
    }
}
