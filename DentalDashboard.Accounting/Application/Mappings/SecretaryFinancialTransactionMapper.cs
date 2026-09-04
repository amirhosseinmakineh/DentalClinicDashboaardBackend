using DentalDashboard.ApplicationService.Contract.Secretary.Accountant.DTOs;
using DentalDashboard.Domain.Secretary.Accountant.Entities;
using DentalDashboard.Domain.Secretary.Accountant.Enums;

namespace DentalDashboard.ApplicationService.Secretary.Accountant.Mappings;

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
