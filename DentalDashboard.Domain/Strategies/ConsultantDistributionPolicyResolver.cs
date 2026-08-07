using DentalDashboard.Domain.Enums;

namespace DentalDashboard.Domain.Strategies;

public static class ConsultantDistributionPolicyResolver
{
    private static readonly IReadOnlyDictionary<ConsultantLevel, ConsultantDistributionPolicy> Policies =
        new Dictionary<ConsultantLevel, ConsultantDistributionPolicy>
        {
            [ConsultantLevel.Test] = new(ConsultantLevel.Test, 0, 20),
            [ConsultantLevel.Seller] = new(ConsultantLevel.Seller, 10, 30),
            [ConsultantLevel.TopSeller] = new(ConsultantLevel.TopSeller, 30, 0)
        };

    public static ConsultantDistributionPolicy Resolve(ConsultantLevel level) =>
        Policies.TryGetValue(level, out var policy)
            ? policy
            : throw new ArgumentOutOfRangeException(nameof(level), level, "Unsupported consultant level.");
}
