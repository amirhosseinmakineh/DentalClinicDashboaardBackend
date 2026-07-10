namespace DentalDashboard.ApplicationService.Contract.IServices;

public record ConsultantDailyLimitStatus
{
    public int EffectiveDailyLimit { get; init; }
    public int TodayPickupCount { get; init; }
    public bool CanPickup { get; init; }

    public string DailyLimitReachedMessage =>
        $"سقف روزانه {EffectiveDailyLimit} لید پر شده است. امروز دیگر نمی‌توانید لید بردارید.";
}
