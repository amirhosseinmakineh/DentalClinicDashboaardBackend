namespace DentalDashboard.ApplicationService.Secretary.Account;

internal static class SecretaryAccountConstants
{
    internal const int DefaultPage = 1;
    internal const int MinimumPageSize = 1;
    internal const int MaximumPageSize = 100;
    internal const string InvalidCurrentUserMessage = "کاربر جاری معتبر نیست";
    internal const string InvalidExpenseCategoryMessage = "دسته‌بندی هزینه معتبر و فعال نیست";
    internal const string TransactionCreatedMessage = "تراکنش مالی با موفقیت ثبت شد";
    internal const string TransactionUpdatedMessage = "تراکنش مالی با موفقیت ویرایش شد";
    internal const string TransactionDeletedMessage = "تراکنش مالی با موفقیت حذف شد";
    internal const string TransactionNotFoundMessage = "تراکنش مالی یافت نشد";
    internal const string IncomeCategoryMustBeEmptyMessage = "برای تراکنش ورودی نباید دسته‌بندی هزینه انتخاب شود";
    internal const string ExpenseCategoryIsRequiredMessage = "دسته‌بندی هزینه الزامی است";
    internal const string ExpenseCategoryNotFoundMessage = "دسته‌بندی هزینه یافت نشد";
    internal const string ExpenseCategoryDuplicateTitleMessage = "دسته‌بندی هزینه‌ای با این عنوان قبلاً ثبت شده است";
    internal const string ExpenseCategoryCreatedMessage = "دسته‌بندی هزینه با موفقیت ایجاد شد";
    internal const string ExpenseCategoryUpdatedMessage = "دسته‌بندی هزینه با موفقیت ویرایش شد";
    internal const string ExpenseCategoryDeletedMessage = "دسته‌بندی هزینه با موفقیت حذف شد";
}
