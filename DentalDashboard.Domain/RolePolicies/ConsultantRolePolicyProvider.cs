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
}
