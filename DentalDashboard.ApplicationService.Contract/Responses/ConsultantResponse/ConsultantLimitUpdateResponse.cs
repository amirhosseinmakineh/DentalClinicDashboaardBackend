namespace DentalDashboard.ApplicationService.Contract.Responses.ConsultantResponse;

using DentalDashboard.ApplicationService.Contract.IServices;

public record ConsultantLimitUpdateResponse
{
    public int? LimitNumber { get; init; }
    public ConsultantDailyLimitsStatus DailyLimits { get; init; } = new();
}
