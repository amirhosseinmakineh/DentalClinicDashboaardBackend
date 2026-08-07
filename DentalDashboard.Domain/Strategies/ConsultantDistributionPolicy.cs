using DentalDashboard.Domain.Enums;

namespace DentalDashboard.Domain.Strategies;

public sealed record ConsultantDistributionPolicy(
    ConsultantLevel Level,
    int RealTimeDailyLimit,
    int BurnedDailyLimit)
{
    public bool AllowsRealTime => RealTimeDailyLimit > 0;
    public bool AllowsBurned => BurnedDailyLimit > 0;
}
