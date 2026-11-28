namespace DentalDashboard.Domain.Accounting.Reminders;

public sealed class AccountingReminderTarget
{
    public long Id { get; init; }
    public Guid UserId { get; init; }
    public AccountingReminderKind Kind { get; init; }
    public long TargetId { get; init; }
    public DateTime DueDate { get; init; }
    public decimal Amount { get; init; }
    public string PatientName { get; init; } = string.Empty;
    public bool IsGuarantee { get; init; }
}

public interface IAccountingReminderRepository
{
    Task<IReadOnlyList<long>> EnabledAsync(Guid userId, AccountingReminderKind kind, long[] ids, CancellationToken ct);
    Task<bool> IsPendingAsync(AccountingReminderKind kind, long targetId, CancellationToken ct);
    Task SetAsync(Guid userId, AccountingReminderKind kind, long targetId, bool enabled, CancellationToken ct);
    Task<IReadOnlyList<AccountingReminderTarget>> DueAsync(DateTime localDate, long afterId, CancellationToken ct);
    Task<bool> StillPendingAsync(AccountingReminderTarget target, CancellationToken ct);
    Task<bool> ClaimAsync(long reminderId, DateTime dueDate, DateTime reminderDate, Guid leaseId, DateTime utcNow, CancellationToken ct);
    Task CompleteAsync(Guid leaseId, bool sent, DateTime utcNow, CancellationToken ct);
}
