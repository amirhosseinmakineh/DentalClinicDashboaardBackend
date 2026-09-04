using DentalDashboard.Utilities.Time;

namespace DentalDashboard.ApplicationService.Secretary.Accountant.PatientFinance.Handlers;

internal static class FinanceRules
{
    private const string PastDueDateMessage =
        "تاریخ سررسید چک یا سفته نمی‌تواند قبل از امروز باشد.";

    public static string? Cheque(
        decimal amount,
        string? sayadNumber,
        string? ownerName,
        DateTime dueDate)
    {
        if (amount <= 0)
        {
            return "مبلغ چک باید بیشتر از صفر باشد";
        }

        if (string.IsNullOrWhiteSpace(sayadNumber))
        {
            return "شماره صیاد الزامی است";
        }

        if (string.IsNullOrWhiteSpace(ownerName))
        {
            return "نام صاحب چک الزامی است";
        }

        return ValidateDueDate(dueDate);
    }

    public static string? Note(
        decimal amount,
        string? serialNumber,
        DateTime dueDate)
    {
        if (amount <= 0)
        {
            return "مبلغ سفته باید بیشتر از صفر باشد";
        }

        if (string.IsNullOrWhiteSpace(serialNumber))
        {
            return "شماره سریال سفته الزامی است";
        }

        return ValidateDueDate(dueDate);
    }

    private static string? ValidateDueDate(DateTime dueDate)
    {
        if (dueDate == default)
        {
            return "تاریخ سررسید الزامی است";
        }

        return IranTimeHelper.GetDateInIran(dueDate) < IranTimeHelper.TodayInIran()
            ? PastDueDateMessage
            : null;
    }
}
