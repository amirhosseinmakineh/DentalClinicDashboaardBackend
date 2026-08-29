namespace DentalDashboard.ApplicationService.Secretary.Account;

internal static class SecretaryAccountConstants
{
    internal const int DefaultPage = 1;
    internal const int MinimumPageSize = 1;
    internal const int MaximumPageSize = 100;
    internal const string InvalidCurrentUserMessage = "کاربر جاری معتبر نیست";
    internal const string InvalidExpenseCategoryMessage = "دسته‌بندی هزینه معتبر و فعال نیست";
    internal const string TransactionCreatedMessage = "تراکنش مالی با موفقیت ثبت شد";
    internal const string IncomeCategoryMustBeEmptyMessage = "برای تراکنش ورودی نباید دسته‌بندی هزینه انتخاب شود";
    internal const string ExpenseCategoryIsRequiredMessage = "دسته‌بندی هزینه الزامی است";
}
