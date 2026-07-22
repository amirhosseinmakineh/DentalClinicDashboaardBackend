namespace DentalDashboard.Utilities.Time;

public static class IranTimeHelper
{
    public static TimeZoneInfo ResolveIranTimeZone()
    {
        foreach (var timeZoneId in new[] { "Asia/Tehran", "Iran Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.CreateCustomTimeZone(
            "Iran",
            TimeSpan.FromHours(3.5),
            "Iran",
            "Iran");
    }

    public static DateTime ToIranLocalTime(DateTime value)
    {
        var utc = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

        return TimeZoneInfo.ConvertTimeFromUtc(utc, ResolveIranTimeZone());
    }

    public static DateTime IranLocalNow => ToIranLocalTime(DateTime.UtcNow);

    public static DateOnly TodayInIran() => DateOnly.FromDateTime(IranLocalNow);

    public static (DateTime Start, DateTime End) GetIranLocalDayRange(DateOnly date)
    {
        var start = date.ToDateTime(TimeOnly.MinValue);
        var end = date.ToDateTime(new TimeOnly(23, 59, 59, 999));
        return (start, end);
    }

    public static (DateTime StartUtc, DateTime EndUtc) GetIranDayRangeAsUtc(DateOnly date)
    {
        var timeZone = ResolveIranTimeZone();
        var startLocal = date.ToDateTime(TimeOnly.MinValue);
        var endLocal = date.ToDateTime(new TimeOnly(23, 59, 59, 999));
        var startUtc = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(startLocal, DateTimeKind.Unspecified),
            timeZone);
        var endUtc = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(endLocal, DateTimeKind.Unspecified),
            timeZone);
        return (startUtc, endUtc);
    }
}
