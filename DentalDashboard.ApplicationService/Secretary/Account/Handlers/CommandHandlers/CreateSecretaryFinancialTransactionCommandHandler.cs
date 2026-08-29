using DentalDashboard.ApplicationService.Secretary.Account;
using DentalDashboard.ApplicationService.Contract.Secretary.Account.Commands;
using DentalDashboard.ApplicationService.Contract.Secretary.Account.DTOs;
using DentalDashboard.ApplicationService.Contract.Secretary.Account.Queries;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Secretary.Account.Entities;
using DentalDashboard.Domain.Secretary.Account.Enums;
using DentalDashboard.Domain.Secretary.Account.Repositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Secretary.Account.Handlers.CommandHandlers;

public sealed class CreateSecretaryFinancialTransactionCommandHandler : ICommandHandler<CreateSecretaryFinancialTransactionCommand, CreateSecretaryFinancialTransactionResponse>
{
    private readonly ISecretaryAccountRepository repository;
    private readonly IUnitOfWork unitOfWork;

    public CreateSecretaryFinancialTransactionCommandHandler(ISecretaryAccountRepository repository, IUnitOfWork unitOfWork)
    {
        this.repository = repository;
        this.unitOfWork = unitOfWork;
    }

    public async Task<Result<CreateSecretaryFinancialTransactionResponse>> HandleAsync(CreateSecretaryFinancialTransactionCommand command, CancellationToken cancellationToken = default)
    {
        if (command.CreatedByUserId == Guid.Empty)
        {
            return Result<CreateSecretaryFinancialTransactionResponse>.Failure(SecretaryAccountConstants.InvalidCurrentUserMessage);
        }

        if (command.Type == FinancialTransactionType.Expense)
        {
            var categoryIsActive = await repository.ExpenseCategories
                .AsNoTracking()
                .AnyAsync(x => x.Id == command.ExpenseCategoryId && !x.IsDeleted && x.IsActive, cancellationToken);

            if (!categoryIsActive)
            {
                return Result<CreateSecretaryFinancialTransactionResponse>.Failure(SecretaryAccountConstants.InvalidExpenseCategoryMessage);
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
            TrackingNumber = Normalize(command.TrackingNumber),
            Description = Normalize(command.Description),
            ReceiptUrl = Normalize(command.ReceiptUrl),
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
