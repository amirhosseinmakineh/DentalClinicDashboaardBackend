using DentalDashboard.ApplicationService.Contract.Accounting;
using DentalDashboard.Domain.Accounting;
using DentalDashboard.Domain.Accounting.Reminders;
using DentalDashboard.ApplicationService.Contract.Accounting.Reminders;
using DentalDashboard.ApplicationService.Contract.IServices;

namespace DentalDashboard.ApplicationService.Accounting.Reminders;

public sealed class AccountingReminderService(IAccountingAccess access, IAccountingReminderRepository repository, IPushNotificationService push)
{
    public async Task<AccountingOperationResponse> TestAsync(TestAccountingReminderCommand command, CancellationToken ct)
    {
        await RequireAccessAsync(command.Actor.UserId, ct);
        var sent = await push.SendAsync(command.Actor.UserId, "تست یادآوری حسابداری", "یادآوری سررسید چک و اقساط برای این حساب فعال است.",
            new Dictionary<string, string> { ["type"] = "AccountingDueReminder", ["userId"] = command.Actor.UserId.ToString(), ["route"] = "/dashboard/accountant", ["tag"] = "accounting-test" }, ct);
        if (!sent) throw new AccountingValidationException("ارسال انجام نشد؛ نوتیفیکیشن را فعال و دوباره امتحان کنید.");
        return new("اعلان آزمایشی به سرویس Push ارسال شد.");
    }

    public async Task<IReadOnlyList<long>> GetAsync(AccountingRemindersQuery query, CancellationToken ct)
    {
        await RequireAccessAsync(query.Actor.UserId, ct);
        ValidateKind(query.Kind);
        if (query.Ids.Length > 100 || query.Ids.Any(x => x <= 0))
            throw new AccountingValidationException("شناسه‌های یادآوری معتبر نیستند.");
        return await repository.EnabledAsync(query.Actor.UserId, query.Kind, query.Ids, ct);
    }

    public async Task<AccountingOperationResponse> SetAsync(SetAccountingReminderCommand command, CancellationToken ct)
    {
        await RequireAccessAsync(command.Actor.UserId, ct);
        ValidateKind(command.Kind);
        if (command.TargetId <= 0 || command.Enabled && !await repository.IsPendingAsync(command.Kind, command.TargetId, ct))
            throw new AccountingValidationException("فقط برای چک در انتظار یا قسط پرداخت‌نشده پرونده فعال می‌توان یادآوری فعال کرد.");
        await repository.SetAsync(command.Actor.UserId, command.Kind, command.TargetId, command.Enabled, ct);
        return new("تنظیم یادآوری ذخیره شد.");
    }

    private async Task RequireAccessAsync(Guid userId, CancellationToken ct)
    {
        if (!await access.AllowsAsync(userId, AccountingPermissions.View, ct)) throw new AccountingForbiddenException();
    }
    private static void ValidateKind(AccountingReminderKind kind)
    {
        if (!Enum.IsDefined(kind)) throw new AccountingValidationException("نوع یادآوری معتبر نیست.");
    }
}
