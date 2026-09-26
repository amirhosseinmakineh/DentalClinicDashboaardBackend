using DentalDashboard.Domain.Secretary.Accountant.Enums;

namespace DentalDashboard.ApplicationService.Contract.Secretary.Accountant.DTOs;

public sealed record SecretaryExpenseCategoryDto(long Id, string Title);

public sealed record SecretaryFinancialSummaryDto(
    decimal TotalIncome,
    decimal TotalExpense,
    decimal Balance,
    int IncomeCount,
    int ExpenseCount);

public sealed record SecretaryFinancialTransactionPage(
    IReadOnlyList<SecretaryFinancialTransactionDto> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed class SecretaryFinancialTransactionDto
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
