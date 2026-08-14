using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.RolePolicies;

namespace DentalDashboard.Tests;

public class ConsultantRolePolicyProviderTests
{
    private readonly ConsultantRolePolicyProvider policies = new();

    public static TheoryData<ConsultantRole, int, ConsultantRole, ConsultantEvaluationResult, int, bool> Decisions => new()
    {
        { ConsultantRole.Test, 0, ConsultantRole.Test, ConsultantEvaluationResult.Deactivated, 0, true },
        { ConsultantRole.Test, 1, ConsultantRole.Seller, ConsultantEvaluationResult.PromotedToSeller, 0, false },
        { ConsultantRole.Seller, 0, ConsultantRole.Test, ConsultantEvaluationResult.DemotedToTest, 0, false },
        { ConsultantRole.Seller, 1, ConsultantRole.Seller, ConsultantEvaluationResult.RemainedSeller, 0, false },
        { ConsultantRole.Seller, 2, ConsultantRole.Seller, ConsultantEvaluationResult.RemainedSeller, 0, false },
        { ConsultantRole.Seller, 3, ConsultantRole.TopSeller, ConsultantEvaluationResult.PromotedToTopSeller, 0, false },
        { ConsultantRole.TopSeller, 3, ConsultantRole.Seller, ConsultantEvaluationResult.DemotedToSeller, 0, false },
        { ConsultantRole.TopSeller, 4, ConsultantRole.TopSeller, ConsultantEvaluationResult.RemainedTopSeller, 0, false },
        { ConsultantRole.TopSeller, 7, ConsultantRole.TopSeller, ConsultantEvaluationResult.TopSellerReward, 1, false },
        { ConsultantRole.TopSeller, 10, ConsultantRole.TopSeller, ConsultantEvaluationResult.TopSellerHigherReward, 2, false }
    };

    [Theory]
    [MemberData(nameof(Decisions))]
    public void Evaluate_returns_expected_decision(
        ConsultantRole role,
        int patients,
        ConsultantRole expectedRole,
        ConsultantEvaluationResult expectedResult,
        int expectedReward,
        bool expectedDeactivation)
    {
        var result = policies.Evaluate(role, patients);

        Assert.Equal(expectedRole, result.ResultingRole);
        Assert.Equal(expectedResult, result.Result);
        Assert.Equal(expectedReward, result.RewardLevel);
        Assert.Equal(expectedDeactivation, result.Deactivate);
    }

    [Fact]
    public void Test_burnt_limit_is_zero_after_reception_period()
    {
        var start = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);

        Assert.Equal(20, policies.GetDailyLimit(ConsultantRole.Test, LeadLimitType.Burnt, start, start.AddDays(5).AddTicks(-1)));
        Assert.Equal(0, policies.GetDailyLimit(ConsultantRole.Test, LeadLimitType.Burnt, start, start.AddDays(5)));
    }

    [Theory]
    [InlineData(ConsultantRole.Test, 0, 20, 10)]
    [InlineData(ConsultantRole.Seller, 10, 30, 10)]
    [InlineData(ConsultantRole.TopSeller, 20, 0, 7)]
    public void Policies_match_production_business_rules(
        ConsultantRole role,
        int realtime,
        int burnt,
        int evaluationDays)
    {
        var policy = policies.Get(role);

        Assert.Equal(realtime, policy.RealtimeDailyLimit);
        Assert.Equal(burnt, policy.BurntDailyLimit);
        Assert.Equal(TimeSpan.FromDays(evaluationDays), policy.EvaluationPeriod);
    }
}
