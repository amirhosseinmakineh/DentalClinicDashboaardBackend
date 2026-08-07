using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.Strategies;

namespace DentalDashboard.Domain.Tests.Strategies;

public sealed class ConsultantDistributionPolicyResolverTests
{
    [Theory]
    [InlineData(ConsultantLevel.Test, 0, 20)]
    [InlineData(ConsultantLevel.Seller, 10, 30)]
    [InlineData(ConsultantLevel.TopSeller, 30, 0)]
    public void Resolves_limits_immediately_from_level(
        ConsultantLevel level, int realtime, int burned)
    {
        var policy = ConsultantDistributionPolicyResolver.Resolve(level);
        Assert.Equal(realtime, policy.RealTimeDailyLimit);
        Assert.Equal(burned, policy.BurnedDailyLimit);
        Assert.Equal(realtime > 0, policy.AllowsRealTime);
        Assert.Equal(burned > 0, policy.AllowsBurned);
    }
}
