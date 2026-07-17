using DentalDashboard.Utilities.Convertor;

namespace DentalDashboard.ApplicationService.Handlers.Helpers;

public static class AttendanceLabels
{
    public static string? ToPersianDateTime(DateOnly date, TimeOnly? time)
    {
        if (!time.HasValue)
            return null;

        return date.ToDateTime(time.Value).ToPersianDateTimeString();
    }

    public static bool IsCurrentlyPresent(TimeOnly? checkInTime, TimeOnly? checkOutTime) =>
        checkInTime.HasValue && !checkOutTime.HasValue;
}
