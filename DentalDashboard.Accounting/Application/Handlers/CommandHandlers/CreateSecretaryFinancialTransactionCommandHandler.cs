using DentalDashboard.Accounting.Application;
using DentalDashboard.Accounting.Contracts.Commands;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Accounting.Domain.Entities;
using DentalDashboard.Accounting.Domain.Enums;
using DentalDashboard.Accounting.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Accounting.Application.Handlers.CommandHandlers;

public sealed class CreateSecretaryFinancialTransactionCommandHandler(
    ISecretaryAccountRepository repository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<
        CreateSecretaryFinancialTransactionCommand,
        CreateSecretaryFinancialTransactionResponse>
{
    public async Task<Result<CreateSecretaryFinancialTransactionResponse>> HandleAsync(
        CreateSecretaryFinancialTransactionCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.CreatedByUserId == Guid.Empty)
        {
            return Result<CreateSecretaryFinancialTransactionResponse>.Failure(
                SecretaryAccountConstants.InvalidCurrentUserMessage);
        }

        if (command.Type == FinancialTransactionType.Expense)
        {
            var categoryIsActive = await repository.ExpenseCategories
                .AsNoTracking()
                .AnyAsync(
                    category =>
                        category.Id == command.ExpenseCategoryId &&
                        !category.IsDeleted &&
                        category.IsActive,
                    cancellationToken);

            if (!categoryIsActive)
            {
                return Result<CreateSecretaryFinancialTransactionResponse>.Failure(
                    SecretaryAccountConstants.InvalidExpenseCategoryMessage);
            }
        }

        var transaction = new FinancialTransaction
        {
            Type = command.Type,
            Amount = command.Amount,
            TransactionDate = command.TransactionDate,
            Subject = Normalize(command.Subject),
            CounterpartyName = Normalize(command.CounterpartyName),
            PaymentMethod = command.PaymentMethod,
            Description = Normalize(command.Description),
            ExpenseCategoryId = command.ExpenseCategoryId,
            CreatedByUserId = command.CreatedByUserId
        };

        await repository.AddTransactionAsync(transaction, cancellationToken);
        await unitOfWork.SaveChangesAsync();

        return Result<CreateSecretaryFinancialTransactionResponse>.Success(
            new CreateSecretaryFinancialTransactionResponse(transaction.Id),
            SecretaryAccountConstants.TransactionCreatedMessage);
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
