using DentalDashboard.Domain.Enums;

namespace DentalDashboard.ApplicationService.Handlers.Helpers;

public static class UserPresenceLabels
{
    private static readonly TimeSpan OnlineThreshold = TimeSpan.FromMinutes(5);

    public static string ToPersianLabel(UserPresenceEventType eventType) =>
        eventType switch
        {
            UserPresenceEventType.Login => "ورود به سیستم",
            UserPresenceEventType.Logout => "خروج از سیستم",
            UserPresenceEventType.Online => "آنلاین شدن",
            UserPresenceEventType.Offline => "آفلاین شدن",
            UserPresenceEventType.CheckIn => "ثبت حضور",
            UserPresenceEventType.CheckOut => "ثبت عدم حضور",
            _ => "نامشخص"
        };

    public static bool IsCurrentlyOnline(DateTime? lastSeenAtUtc)
    {
        if (!lastSeenAtUtc.HasValue)
            return false;

        return DateTime.UtcNow - lastSeenAtUtc.Value < OnlineThreshold;
    }

    public static (DateTime Start, DateTime End) GetDayRange(DateOnly date)
    {
        var start = date.ToDateTime(TimeOnly.MinValue);
        var end = date.ToDateTime(new TimeOnly(23, 59, 59, 999));
        return (start, end);
    }
}
