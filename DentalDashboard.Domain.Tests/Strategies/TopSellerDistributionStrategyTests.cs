using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.Strategies;
using Xunit;

namespace DentalDashboard.Domain.Tests.Strategies;

public sealed class TopSellerDistributionStrategyTests
{
    private static readonly DateTime Start = new(2026, 8, 1);
    private readonly TopSellerDistributionStrategy strategy = new(TopSellerPolicy.Default);

    [Theory]
    [InlineData(0, true, 30)]
    [InlineData(29, true, 1)]
    [InlineData(30, false, 0)]
    public void Enforces_realtime_limit_and_never_allows_burned(
        int assigned, bool allowed, int remaining)
    {
        var decision = Decide(Start, assigned, 0);
        Assert.Equal(allowed, decision.CanReceiveRealTimeLead);
        Assert.Equal(remaining, decision.RemainingRealTimeCapacity);
        Assert.False(decision.CanReceiveBurnedLead);
    }

    [Fact]
    public void Full_seventh_day_is_not_ready() =>
        Assert.False(Decide(Start.AddDays(6).AddHours(23).AddMinutes(59), 0, 15)
            .IsReadyForWeeklyEvaluation);

    [Theory]
    [InlineData(0, false, true, TopSellerRewardLevel.None)]
    [InlineData(1, false, true, TopSellerRewardLevel.None)]
    [InlineData(3, false, true, TopSellerRewardLevel.None)]
    [InlineData(4, true, false, TopSellerRewardLevel.None)]
    [InlineData(6, true, false, TopSellerRewardLevel.None)]
    [InlineData(7, true, false, TopSellerRewardLevel.Level1)]
    [InlineData(9, true, false, TopSellerRewardLevel.Level1)]
    [InlineData(10, true, false, TopSellerRewardLevel.Level2)]
    [InlineData(15, true, false, TopSellerRewardLevel.Level2)]
    public void Evaluates_weekly_thresholds(int patients, bool remains, bool downgrade,
        TopSellerRewardLevel reward)
    {
        var decision = Decide(Start.AddDays(7), 0, patients);
        Assert.True(decision.IsReadyForWeeklyEvaluation);
        Assert.Equal(remains, decision.ShouldRemainTopSeller);
        Assert.Equal(downgrade, decision.ShouldDowngradeToSeller);
        Assert.Equal(reward, decision.RewardLevel);
    }

    private TopSellerDecision Decide(DateTime now, int assigned, int patients) =>
        strategy.Decide(new TopSellerContext
        {
            TopSellerStartedAt = Start,
            CurrentTime = now,
            AssignedRealTimeToday = assigned,
            SuccessfulPatients = patients,
            IsActive = true,
            IsAvailable = true,
            IsOnline = true
        });
}
