using DentalDashboard.Accounting.Domain.Enums;

namespace DentalDashboard.Accounting.Contracts.DTOs;

public sealed record SecretaryFinancialSummaryDto(
    decimal TotalIncome,
    decimal TotalExpense,
    decimal Balance,
    int IncomeCount,
    int ExpenseCount);
