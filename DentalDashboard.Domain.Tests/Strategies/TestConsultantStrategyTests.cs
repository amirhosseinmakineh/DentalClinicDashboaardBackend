using DentalDashboard.Domain.Strategies;

namespace DentalDashboard.Domain.Tests.Strategies;

public sealed class TestConsultantStrategyTests
{
    private readonly TestConsultantStrategy strategy = new(TestConsultantPolicy.Default);
    private static readonly DateTime Start = new(2026, 8, 1, 9, 0, 0);

    [Theory]
    [InlineData(0, 0, true, 20)]
    [InlineData(4, 19, true, 1)]
    [InlineData(4, 20, false, 0)]
    [InlineData(5, 0, false, 0)]
    [InlineData(9, 0, false, 0)]
    public void Distribution_obeys_phase_and_daily_limit(
        int elapsedDays, int assignedToday, bool canReceive, int remaining)
    {
        var decision = strategy.Decide(Context(Start.AddDays(elapsedDays), assignedToday));

        Assert.Equal(canReceive, decision.CanReceiveNewLead);
        Assert.Equal(remaining, decision.RemainingDailyCapacity);
    }

    [Fact]
    public void Day_ten_is_still_follow_up_and_not_ready_for_evaluation()
    {
        var decision = strategy.Decide(Context(Start.Date.AddDays(9).AddHours(23).AddMinutes(59), 0));

        Assert.True(decision.IsFollowUpPhase);
        Assert.False(decision.IsReadyForEvaluation);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    public void Evaluation_after_full_day_ten_uses_confirmed_patient_threshold(
        int confirmedPatients, bool passed)
    {
        var decision = strategy.Decide(Context(Start.Date.AddDays(11), 0, confirmedPatients));

        Assert.True(decision.IsReadyForEvaluation);
        Assert.Equal(passed, decision.HasPassed);
    }

    private static TestConsultantContext Context(DateTime now, int assignedToday, int confirmed = 0) => new()
    {
        TestStartedAt = Start,
        CurrentTime = now,
        AssignedTodayCount = assignedToday,
        ConfirmedPatientCount = confirmed,
        IsActive = true,
        IsAvailable = true,
        IsOnline = true
    };
}
