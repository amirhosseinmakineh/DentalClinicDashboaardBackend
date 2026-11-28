using DentalDashboard.Domain.Accounting.Reminders;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Enums;
using DentalDashboard.Infrastracture.Context;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using DentalDashboard.Utilities.Time;

namespace DentalDashboard.Infrastracture.Accounting.Reminders;

public sealed class AccountingReminderRepository(DentalContext db) : IAccountingReminderRepository
{
    private IQueryable<AccountingReminderTarget> PendingTargets =>
        (from r in db.Set<AccountingReminder>().AsNoTracking()
         join c in db.PatientCheques on r.TargetId equals c.Id
         where r.Enabled && r.Kind == AccountingReminderKind.Cheque && !c.IsDeleted &&
             c.Status == PatientChequeStatus.Pending && !c.FinancialCase.IsDeleted &&
             c.FinancialCase.Status == PatientFinancialCaseStatus.Active
         select new AccountingReminderTarget { Id = r.Id, UserId = r.UserId, Kind = r.Kind, TargetId = c.Id, DueDate = c.DueDate, Amount = c.Amount,
             PatientName = c.FinancialCase.Patient.FirstName + " " + c.FinancialCase.Patient.LastName, IsGuarantee = c.IsGuarantee })
        .Concat(
         from r in db.Set<AccountingReminder>().AsNoTracking()
         join i in db.PatientInstallments on r.TargetId equals i.Id
         where r.Enabled && r.Kind == AccountingReminderKind.Installment && !i.IsDeleted && i.PaidAtUtc == null &&
             !i.FinancialCase.IsDeleted && i.FinancialCase.Status == PatientFinancialCaseStatus.Active
         select new AccountingReminderTarget { Id = r.Id, UserId = r.UserId, Kind = r.Kind, TargetId = i.Id, DueDate = i.DueDate, Amount = i.Amount,
             PatientName = i.FinancialCase.Patient.FirstName + " " + i.FinancialCase.Patient.LastName, IsGuarantee = false });

    public async Task<IReadOnlyList<long>> EnabledAsync(Guid userId, AccountingReminderKind kind, long[] ids, CancellationToken ct) =>
        await db.Set<AccountingReminder>().AsNoTracking().Where(x => x.UserId == userId && x.Kind == kind && x.Enabled && ids.Contains(x.TargetId))
            .Select(x => x.TargetId).ToListAsync(ct);

    public Task<bool> IsPendingAsync(AccountingReminderKind kind, long targetId, CancellationToken ct) =>
        kind == AccountingReminderKind.Cheque
        ? db.PatientCheques.AnyAsync(x => x.Id == targetId && !x.IsDeleted && x.Status == PatientChequeStatus.Pending &&
            !x.FinancialCase.IsDeleted && x.FinancialCase.Status == PatientFinancialCaseStatus.Active, ct)
        : db.PatientInstallments.AnyAsync(x => x.Id == targetId && !x.IsDeleted && x.PaidAtUtc == null &&
            !x.FinancialCase.IsDeleted && x.FinancialCase.Status == PatientFinancialCaseStatus.Active, ct);

    public async Task SetAsync(Guid userId, AccountingReminderKind kind, long targetId, bool enabled, CancellationToken ct)
    {
        var row = await db.Set<AccountingReminder>().SingleOrDefaultAsync(x => x.UserId == userId && x.Kind == kind && x.TargetId == targetId, ct);
        if (row == null)
        {
            if (!enabled) return;
            row = new() { UserId = userId, Kind = kind, TargetId = targetId };
            db.Add(row);
        }
        row.Enabled = enabled;
        row.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<AccountingReminderTarget>> DueAsync(DateTime localDate, long afterId, CancellationToken ct)
    {
        var (start, _) = IranTimeHelper.GetIranDayRangeAsUtc(DateOnly.FromDateTime(localDate));
        var (_, end) = IranTimeHelper.GetIranDayRangeAsUtc(DateOnly.FromDateTime(localDate.AddDays(1)));
        return await PendingTargets.Where(x => x.Id > afterId && x.DueDate >= start && x.DueDate < end)
            .OrderBy(x => x.Id).Take(200).ToListAsync(ct);
    }

    public Task<bool> StillPendingAsync(AccountingReminderTarget target, CancellationToken ct) =>
        PendingTargets.AnyAsync(x => x.Id == target.Id && x.UserId == target.UserId && x.DueDate == target.DueDate && x.Amount == target.Amount, ct);

    public async Task<bool> ClaimAsync(long reminderId, DateTime dueDate, DateTime reminderDate, Guid leaseId, DateTime utcNow, CancellationToken ct)
    {
        var rows = db.Set<AccountingReminderDelivery>();
        var existing = await rows.AsNoTracking().AnyAsync(x => x.ReminderId == reminderId && x.DueDate == dueDate && x.ReminderDate == reminderDate, ct);
        if (!existing)
        {
            var delivery = new AccountingReminderDelivery { ReminderId = reminderId, DueDate = dueDate, ReminderDate = reminderDate };
            db.Add(delivery);
            try { await db.SaveChangesAsync(ct); }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 }) { db.Entry(delivery).State = EntityState.Detached; }
        }
        return await rows.Where(x => x.ReminderId == reminderId && x.DueDate == dueDate && x.ReminderDate == reminderDate &&
                x.SentAtUtc == null && (x.LeaseUntilUtc == null || x.LeaseUntilUtc <= utcNow))
            .ExecuteUpdateAsync(update => update.SetProperty(x => x.LeaseId, leaseId)
                .SetProperty(x => x.LeaseUntilUtc, utcNow.AddMinutes(10)).SetProperty(x => x.Attempts, x => x.Attempts + 1), ct) == 1;
    }

    public async Task CompleteAsync(Guid leaseId, bool sent, DateTime utcNow, CancellationToken ct)
    {
        await db.Set<AccountingReminderDelivery>().Where(x => x.LeaseId == leaseId && x.SentAtUtc == null)
            .ExecuteUpdateAsync(update => update.SetProperty(x => x.SentAtUtc, sent ? utcNow : (DateTime?)null)
                .SetProperty(x => x.LeaseUntilUtc, utcNow.AddMinutes(15)), ct);
    }
}
