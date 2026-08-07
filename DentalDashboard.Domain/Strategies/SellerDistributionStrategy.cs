namespace DentalDashboard.Domain.Strategies;

public sealed class SellerDistributionStrategy
{
    private readonly SellerConsultantPolicy policy;

    public SellerDistributionStrategy(SellerConsultantPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        if (policy.NewLeadDailyLimit <= 0 || policy.BurnedLeadDailyLimit <= 0 ||
            policy.EvaluationDays <= 0 || policy.GoldConfirmedPatientThreshold <= 0)
            throw new ArgumentOutOfRangeException(nameof(policy));
        this.policy = policy;
    }

    public SellerConsultantDecision Decide(SellerConsultantContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var start = context.SellerStartedAt.Date;
        var currentDay = context.CurrentTime.Date < start
            ? 0
            : (context.CurrentTime.Date - start).Days + 1;
        var newCapacity = Math.Max(0, policy.NewLeadDailyLimit - Math.Max(0, context.AssignedNewLeadToday));
        var burnedCapacity = Math.Max(0, policy.BurnedLeadDailyLimit - Math.Max(0, context.AssignedBurnedLeadToday));
        // Midnight following day ten is the first evaluation instant.
        var ready = context.CurrentTime >= start.AddDays(policy.EvaluationDays);
        var operational = context.IsActive && context.IsAvailable && context.IsOnline;
        var promote = ready && context.ConfirmedPatientCount >= policy.GoldConfirmedPatientThreshold;
        var returnToTest = ready && context.ConfirmedPatientCount <= 0;

        return new SellerConsultantDecision(
            currentDay,
            operational && newCapacity > 0,
            operational && burnedCapacity > 0,
            newCapacity,
            burnedCapacity,
            ready,
            promote,
            ready && !promote && !returnToTest,
            returnToTest);
    }
}
