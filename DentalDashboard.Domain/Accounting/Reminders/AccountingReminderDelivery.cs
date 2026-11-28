namespace DentalDashboard.Domain.Accounting.Reminders;

public sealed class AccountingReminderDelivery
{
    public long Id { get; set; }
    public long ReminderId { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime ReminderDate { get; set; }
    public Guid? LeaseId { get; set; }
    public DateTime? LeaseUntilUtc { get; set; }
    public DateTime? SentAtUtc { get; set; }
    public int Attempts { get; set; }
}
