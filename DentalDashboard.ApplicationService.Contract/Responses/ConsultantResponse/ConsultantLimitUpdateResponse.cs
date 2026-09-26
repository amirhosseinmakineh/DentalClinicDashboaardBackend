namespace DentalDashboard.ApplicationService.Contract.Responses.ConsultantResponse;

public record ConsultantLimitUpdateResponse
{
    public int? LimitNumber { get; init; }
    public int EffectiveDailyLimit { get; init; }
    public int TodayPickupCount { get; init; }
}
