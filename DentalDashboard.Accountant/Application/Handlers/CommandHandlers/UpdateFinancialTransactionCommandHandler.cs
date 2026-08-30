using DentalDashboard.Accountant.Application.Contracts.Commands;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Accountant.Domain.Enums;
using DentalDashboard.Accountant.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Accountant.Application.Handlers.CommandHandlers;

public sealed class UpdateFinancialTransactionCommandHandler
    : ICommandHandler<UpdateFinancialTransactionCommand, CreateFinancialTransactionResponse>
{
    private readonly IAccountantRepository repository;
    private readonly IUnitOfWork unitOfWork;

    public UpdateFinancialTransactionCommandHandler(
        IAccountantRepository repository,
        IUnitOfWork unitOfWork)
    {
        this.repository = repository;
        this.unitOfWork = unitOfWork;
    }

    public async Task<Result<CreateFinancialTransactionResponse>> HandleAsync(
        UpdateFinancialTransactionCommand command,
        CancellationToken cancellationToken = default)
    {
        var transaction = await repository.FinancialTransactions
            .FirstOrDefaultAsync(x => x.Id == command.Id && !x.IsDeleted, cancellationToken);
        if (transaction is null)
        {
            return Result<CreateFinancialTransactionResponse>.Failure(
                AccountantConstants.TransactionNotFoundMessage);
        }

        if (command.Type == FinancialTransactionType.Expense)
        {
            var categoryIsActive = await repository.ExpenseCategories
                .AsNoTracking()
                .AnyAsync(x => x.Id == command.ExpenseCategoryId && !x.IsDeleted && x.IsActive, cancellationToken);
            if (!categoryIsActive)
            {
                return Result<CreateFinancialTransactionResponse>.Failure(
                    AccountantConstants.InvalidExpenseCategoryMessage);
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

        return Result<CreateFinancialTransactionResponse>.Success(
            new CreateFinancialTransactionResponse(transaction.Id),
            AccountantConstants.TransactionUpdatedMessage);
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
