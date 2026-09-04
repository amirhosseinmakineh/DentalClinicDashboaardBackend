using System.Globalization;
using DentalDashboard.ApplicationService.Contract.Secretary.Accountant.PatientFinance.Queries;

namespace DentalDashboard.ApplicationService.Secretary.Accountant.PatientFinance.Handlers;

internal static class QueryTools
{
    public static (int PageNumber, int PageSize) Page(PatientFinancePagedQuery request)
    {
        return (
            Math.Max(1, request.Page),
            Math.Clamp(request.PageSize, 1, 100));
    }

    public static string Name(string firstName, string lastName)
    {
        return (firstName + " " + lastName).Trim();
    }

    public static (DateTime start, DateTime end)? PersianMonth(int? year, int? month)
    {
        if (year is null && month is null)
            return null;

        if (year is null || month is null || month < 1 || month > 12)
            throw new ArgumentException("سال و ماه باید با هم و معتبر ارسال شوند");

        var persianCalendar = new PersianCalendar();

        var start = DateTime.SpecifyKind(
            persianCalendar.ToDateTime(year.Value, month.Value, 1, 0, 0, 0, 0),
            DateTimeKind.Utc);

        var daysInMonth = persianCalendar.GetDaysInMonth(year.Value, month.Value);

        var end = DateTime.SpecifyKind(
            persianCalendar.ToDateTime(year.Value, month.Value, daysInMonth, 23, 59, 59, 999, 0),
            DateTimeKind.Utc);

        return (start, end);
    }
}
