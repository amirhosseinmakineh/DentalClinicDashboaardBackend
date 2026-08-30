using DentalDashboard.Domain.Models;
using DentalDashboard.Domain.Secretary.Account.Enums;

namespace DentalDashboard.Domain.Secretary.Account.Entities;

public sealed class FinancialTransaction : BaseAuditableEntity<long>
{
    public FinancialTransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public DateTime TransactionDate { get; set; }
    public string? Subject { get; set; }
    public string? CounterpartyName { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string? Description { get; set; }
    public long? ExpenseCategoryId { get; set; }
    public ExpenseCategory? ExpenseCategory { get; set; }
    public Guid CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = default!;
}
