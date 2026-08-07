namespace DentalDashboard.Domain.Strategies;

public sealed class TestConsultantStrategy : ITestConsultantStrategy
{
    private readonly TestConsultantPolicy policy;

    public TestConsultantStrategy(TestConsultantPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        if (policy.DailyLeadLimit <= 0 || policy.DistributionDays <= 0 ||
            policy.TotalTestDays < policy.DistributionDays || policy.RequiredConfirmedPatients <= 0)
            throw new ArgumentOutOfRangeException(nameof(policy));

        this.policy = policy;
    }

    public TestConsultantDecision Decide(TestConsultantContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var startDate = context.TestStartedAt.Date;
        var currentDate = context.CurrentTime.Date;
        var currentDay = currentDate < startDate
            ? 0
            : (currentDate - startDate).Days + 1;
        var remaining = Math.Max(0, policy.DailyLeadLimit - Math.Max(0, context.AssignedTodayCount));
        var isDistributionPhase = currentDay >= 1 && currentDay <= policy.DistributionDays;
        var isFollowUpPhase = currentDay > policy.DistributionDays &&
                              currentDay <= policy.TotalTestDays;
        var evaluationAt = startDate.AddDays(policy.TotalTestDays);
        var isReadyForEvaluation = context.CurrentTime >= evaluationAt;

        return new TestConsultantDecision(
            currentDay,
            isDistributionPhase && remaining > 0 && context.IsActive &&
            context.IsAvailable && context.IsOnline,
            isDistributionPhase ? remaining : 0,
            isFollowUpPhase,
            isReadyForEvaluation,
            isReadyForEvaluation && context.ConfirmedPatientCount >= policy.RequiredConfirmedPatients);
    }
}
