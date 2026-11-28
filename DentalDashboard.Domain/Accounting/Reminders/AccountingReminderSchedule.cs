namespace DentalDashboard.Domain.Accounting.Reminders;

public static class AccountingReminderSchedule
{
    private static readonly TimeZoneInfo Tehran = TimeZoneInfo.FindSystemTimeZoneById("Asia/Tehran");

    public static DateTime LocalNow(DateTime utcNow) => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc), Tehran);
    public static bool CanSend(DateTime utcNow, DateTime dueDate)
    {
        var local = LocalNow(utcNow);
        var days = (LocalNow(dueDate).Date - local.Date).Days;
        return local.Hour >= 9 && days is 0 or 1;
    }
}
