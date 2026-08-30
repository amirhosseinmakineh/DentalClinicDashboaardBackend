using DentalDashboard.ApplicationService.Accountant;
using DentalDashboard.ApplicationService.Contract.Accountant.Commands;
using DentalDashboard.ApplicationService.Contract.Accountant.DTOs;
using DentalDashboard.ApplicationService.Contract.Accountant.Queries;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Accountant.Entities;
using DentalDashboard.Domain.Accountant.Enums;
using DentalDashboard.Domain.Accountant.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Accountant.Handlers.CommandHandlers;

public sealed class CreateFinancialTransactionCommandHandler : ICommandHandler<CreateFinancialTransactionCommand, CreateFinancialTransactionResponse>
{
    private readonly IAccountantRepository repository;
    private readonly IUnitOfWork unitOfWork;

    public CreateFinancialTransactionCommandHandler(IAccountantRepository repository, IUnitOfWork unitOfWork)
    {
        this.repository = repository;
        this.unitOfWork = unitOfWork;
    }

    public async Task<Result<CreateFinancialTransactionResponse>> HandleAsync(CreateFinancialTransactionCommand command, CancellationToken cancellationToken = default)
    {
        if (command.CreatedByUserId == Guid.Empty)
        {
            return Result<CreateFinancialTransactionResponse>.Failure(AccountantConstants.InvalidCurrentUserMessage);
        }

        if (command.Type == FinancialTransactionType.Expense)
        {
            var categoryIsActive = await repository.ExpenseCategories
                .AsNoTracking()
                .AnyAsync(x => x.Id == command.ExpenseCategoryId && !x.IsDeleted && x.IsActive, cancellationToken);

            if (!categoryIsActive)
            {
                return Result<CreateFinancialTransactionResponse>.Failure(AccountantConstants.InvalidExpenseCategoryMessage);
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

        return Result<CreateFinancialTransactionResponse>.Success(
            new CreateFinancialTransactionResponse(transaction.Id),
            AccountantConstants.TransactionCreatedMessage);
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
