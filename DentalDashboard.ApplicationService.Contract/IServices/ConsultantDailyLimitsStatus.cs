namespace DentalDashboard.ApplicationService.Contract.IServices;

public record ConsultantDailyLimitsStatus
{
    public ConsultantDailyLimitStatus? Realtime { get; init; }
    public ConsultantDailyLimitStatus? Burnt { get; init; }
}
