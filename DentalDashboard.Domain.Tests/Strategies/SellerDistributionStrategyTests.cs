using DentalDashboard.Domain.Strategies;
using Xunit;

namespace DentalDashboard.Domain.Tests.Strategies;

public class SellerDistributionStrategyTests
{
    private readonly SellerDistributionStrategy strategy = new(SellerConsultantPolicy.Default);
    private static readonly DateTime Start = new(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void DayOne_CanReceiveBothLeadKinds() => AssertDecision(Start, 0, 0, 0, d =>
    {
        Assert.True(d.CanReceiveNewLead); Assert.True(d.CanReceiveBurnedLead);
        Assert.Equal(10, d.RemainingNewLeadCapacity); Assert.Equal(30, d.RemainingBurnedLeadCapacity);
    });

    [Fact]
    public void Quotas_AreNeverExceeded() => AssertDecision(Start, 10, 30, 0, d =>
    {
        Assert.False(d.CanReceiveNewLead); Assert.False(d.CanReceiveBurnedLead);
        Assert.Equal(0, d.RemainingNewLeadCapacity); Assert.Equal(0, d.RemainingBurnedLeadCapacity);
    });

    [Fact]
    public void FullTenthDay_IsNotEvaluationReady() =>
        AssertDecision(Start.AddDays(9).AddHours(23).AddMinutes(59), 0, 0, 3,
            d => Assert.False(d.IsReadyForEvaluation));

    [Theory]
    [InlineData(0, false, false, true)]
    [InlineData(1, false, true, false)]
    [InlineData(2, false, true, false)]
    [InlineData(3, true, false, false)]
    [InlineData(8, true, false, false)]
    public void AfterDayTen_EvaluatesOutcome(int patients, bool gold, bool seller, bool test) =>
        AssertDecision(Start.AddDays(10), 0, 0, patients, d =>
        {
            Assert.True(d.IsReadyForEvaluation);
            Assert.Equal(gold, d.ShouldPromoteToGold);
            Assert.Equal(seller, d.ShouldRemainSeller);
            Assert.Equal(test, d.ShouldReturnToTest);
        });

    private void AssertDecision(DateTime now, int newCount, int burnedCount, int patients,
        Action<SellerConsultantDecision> assertion) => assertion(strategy.Decide(new SellerConsultantContext
        {
            SellerStartedAt = Start, CurrentTime = now, AssignedNewLeadToday = newCount,
            AssignedBurnedLeadToday = burnedCount, ConfirmedPatientCount = patients,
            IsActive = true, IsAvailable = true, IsOnline = true
        }));
}
