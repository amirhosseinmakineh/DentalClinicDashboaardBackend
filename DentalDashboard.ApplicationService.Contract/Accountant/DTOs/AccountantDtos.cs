using DentalDashboard.Domain.Accountant.Enums;

namespace DentalDashboard.ApplicationService.Contract.Accountant.DTOs;

public sealed record ExpenseCategoryDto(long Id, string Title);

public sealed record FinancialSummaryDto(
    decimal TotalIncome,
    decimal TotalExpense,
    decimal Balance,
    int IncomeCount,
    int ExpenseCount);

public sealed record FinancialTransactionPage(
    IReadOnlyList<FinancialTransactionDto> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed class FinancialTransactionDto
{
    public long Id { get; init; }
    public FinancialTransactionType Type { get; init; }
    public string TypeTitle { get; init; } = default!;
    public decimal Amount { get; init; }
    public DateTime TransactionDate { get; init; }
    public string? Subject { get; init; }
    public string? CounterpartyName { get; init; }
    public PaymentMethod PaymentMethod { get; init; }
    public string PaymentMethodTitle { get; init; } = default!;
    public string? Description { get; init; }
    public long? ExpenseCategoryId { get; init; }
    public string? ExpenseCategoryTitle { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
