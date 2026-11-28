using System.Globalization;
using DentalDashboard.ApplicationService.Contract.Accounting;
using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.Domain.Accounting;
using DentalDashboard.Domain.Accounting.Reminders;
using Microsoft.Extensions.Logging;

namespace DentalDashboard.ApplicationService.Accounting.Reminders;

public sealed class AccountingReminderDispatcher(IAccountingReminderRepository repository, IAccountingAccess access,
    IPushNotificationService push, ILogger<AccountingReminderDispatcher> logger)
{
    public async Task DispatchAsync(DateTime utcNow, CancellationToken ct)
    {
        var today = AccountingReminderSchedule.LocalNow(utcNow).Date;
        if (AccountingReminderSchedule.LocalNow(utcNow).Hour < 9) return;
        long after = 0;
        while (!ct.IsCancellationRequested)
        {
            var targets = await repository.DueAsync(today, after, ct);
            if (targets.Count == 0) break;
            foreach (var target in targets)
            {
                after = target.Id;
                try
                {
                    if (!AccountingReminderSchedule.CanSend(utcNow, target.DueDate) ||
                        !await access.AllowsAsync(target.UserId, AccountingPermissions.View, ct) ||
                        !await repository.StillPendingAsync(target, ct)) continue;
                    var lease = Guid.NewGuid();
                    var dueDate = AccountingReminderSchedule.LocalNow(target.DueDate).Date;
                    if (!await repository.ClaimAsync(target.Id, dueDate, today, lease, utcNow, ct)) continue;
                    if (!await repository.StillPendingAsync(target, ct)) continue;
                    var name = target.Kind == AccountingReminderKind.Installment ? "قسط" : target.IsGuarantee ? "چک ضمانت" : "چک";
                    var when = dueDate == today ? "امروز" : "فردا";
                    var amount = target.Amount.ToString("N0", CultureInfo.GetCultureInfo("fa-IR"));
                    var section = target.Kind == AccountingReminderKind.Cheque ? "Cheques" : "Installments";
                    var sent = await push.SendAsync(target.UserId, $"یادآوری سررسید {name}",
                        $"{name} به مبلغ {amount} تومان برای {target.PatientName}، {when} سررسید می‌شود.",
                        new Dictionary<string, string> {
                            ["type"] = "AccountingDueReminder", ["reminderId"] = target.Id.ToString(),
                            ["userId"] = target.UserId.ToString(),
                            ["route"] = $"/dashboard/accountant?section={section}&targetId={target.TargetId}",
                            ["tag"] = $"accounting-{target.Id}-{today:yyyyMMdd}"
                        }, ct);
                    await repository.CompleteAsync(lease, sent, DateTime.UtcNow, ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
                catch (Exception ex) { logger.LogError(ex, "Accounting reminder {ReminderId} failed", target.Id); }
            }
        }
    }
}
