namespace DentalDashboard.Domain.Accounting.Reminders;

public enum AccountingReminderKind { Cheque = 1, Installment = 2 }

public sealed class AccountingReminder
{
    public long Id { get; set; }
    public Guid UserId { get; set; }
    public AccountingReminderKind Kind { get; set; }
    public long TargetId { get; set; }
    public bool Enabled { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
