using DentalDashboard.ApplicationService.Contract.Secretary.Accountant.Commands;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Secretary.Accountant.Enums;
using DentalDashboard.Domain.Secretary.Accountant.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Secretary.Accountant.Handlers.CommandHandlers;

public sealed class UpdateSecretaryFinancialTransactionCommandHandler
    : ICommandHandler<UpdateSecretaryFinancialTransactionCommand, CreateSecretaryFinancialTransactionResponse>
{
    private readonly ISecretaryAccountRepository repository;
    private readonly IUnitOfWork unitOfWork;

    public UpdateSecretaryFinancialTransactionCommandHandler(
        ISecretaryAccountRepository repository,
        IUnitOfWork unitOfWork)
    {
        this.repository = repository;
        this.unitOfWork = unitOfWork;
    }

    public async Task<Result<CreateSecretaryFinancialTransactionResponse>> HandleAsync(
        UpdateSecretaryFinancialTransactionCommand command,
        CancellationToken cancellationToken = default)
    {
        var transaction = await repository.FinancialTransactions
            .FirstOrDefaultAsync(item => item.Id == command.Id && !item.IsDeleted, cancellationToken);
        if (transaction is null)
        {
            return Result<CreateSecretaryFinancialTransactionResponse>.Failure(
                SecretaryAccountConstants.TransactionNotFoundMessage);
        }

        if (command.Type == FinancialTransactionType.Expense)
        {
            var categoryIsActive = await repository.ExpenseCategories
                .AsNoTracking()
                .AnyAsync(item => item.Id == command.ExpenseCategoryId && !item.IsDeleted && item.IsActive, cancellationToken);
            if (!categoryIsActive)
            {
                return Result<CreateSecretaryFinancialTransactionResponse>.Failure(
                    SecretaryAccountConstants.InvalidExpenseCategoryMessage);
            }
        }

        transaction.Type = command.Type;
        transaction.Amount = command.Amount;
        transaction.TransactionDate = command.TransactionDate;
        transaction.Subject = Normalize(command.Subject);
        transaction.CounterpartyName = Normalize(command.CounterpartyName);
        transaction.PaymentMethod = command.PaymentMethod;
        transaction.Description = Normalize(command.Description);
        transaction.ExpenseCategoryId = command.ExpenseCategoryId;
        transaction.UpdatedAt = DateTime.UtcNow;

        await unitOfWork.SaveChangesAsync();

        return Result<CreateSecretaryFinancialTransactionResponse>.Success(
            new CreateSecretaryFinancialTransactionResponse(transaction.Id),
            SecretaryAccountConstants.TransactionUpdatedMessage);
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
