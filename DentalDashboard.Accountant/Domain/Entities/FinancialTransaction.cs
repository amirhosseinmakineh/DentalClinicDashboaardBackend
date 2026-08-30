using DentalDashboard.Domain.Models;
using DentalDashboard.Accountant.Domain.Enums;

namespace DentalDashboard.Accountant.Domain.Entities;

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
