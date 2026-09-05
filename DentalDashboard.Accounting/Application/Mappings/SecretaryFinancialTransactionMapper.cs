using DentalDashboard.Accounting.Contracts.DTOs;
using DentalDashboard.Accounting.Domain.Entities;
using DentalDashboard.Accounting.Domain.Enums;

namespace DentalDashboard.Accounting.Application.Mappings;

internal static class SecretaryFinancialTransactionMapper
{
    public static SecretaryFinancialTransactionDto ToDto(
        FinancialTransaction transaction)
    {
        return new SecretaryFinancialTransactionDto
        {
            Id = transaction.Id,
            Type = transaction.Type,
            TypeTitle = transaction.Type.GetTitle(),
            Amount = transaction.Amount,
            TransactionDate = transaction.TransactionDate,
            Subject = transaction.Subject,
            CounterpartyName = transaction.CounterpartyName,
            PaymentMethod = transaction.PaymentMethod,
            PaymentMethodTitle = transaction.PaymentMethod.GetTitle(),
            Description = transaction.Description,
            ExpenseCategoryId = transaction.ExpenseCategoryId,
            ExpenseCategoryTitle = transaction.ExpenseCategory?.Title,
            CreatedAt = transaction.CreatedAt,
            UpdatedAt = transaction.UpdatedAt
        };
    }
}
