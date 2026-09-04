using DentalDashboard.Domain.Secretary.Accountant.Enums;

namespace DentalDashboard.ApplicationService.Contract.Secretary.Accountant.DTOs;

public sealed record SecretaryFinancialSummaryDto(
    decimal TotalIncome,
    decimal TotalExpense,
    decimal Balance,
    int IncomeCount,
    int ExpenseCount);
