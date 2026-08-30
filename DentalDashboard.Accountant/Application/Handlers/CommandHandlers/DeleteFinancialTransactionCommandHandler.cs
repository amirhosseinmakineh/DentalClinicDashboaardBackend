using DentalDashboard.Accountant.Application.Contracts.Commands;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Accountant.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Accountant.Application.Handlers.CommandHandlers;

public sealed class DeleteFinancialTransactionCommandHandler
    : ICommandHandler<DeleteFinancialTransactionCommand>
{
    private readonly IAccountantRepository repository;
    private readonly IUnitOfWork unitOfWork;

    public DeleteFinancialTransactionCommandHandler(
        IAccountantRepository repository,
        IUnitOfWork unitOfWork)
    {
        this.repository = repository;
        this.unitOfWork = unitOfWork;
    }

    public async Task<Result> HandleAsync(
        DeleteFinancialTransactionCommand command,
        CancellationToken cancellationToken = default)
    {
        var transaction = await repository.FinancialTransactions
            .FirstOrDefaultAsync(x => x.Id == command.Id && !x.IsDeleted, cancellationToken);
        if (transaction is null)
        {
            return Result.Failure(AccountantConstants.TransactionNotFoundMessage);
        }

        var now = DateTime.UtcNow;
        transaction.IsDeleted = true;
        transaction.DeletedAt = now;
        transaction.UpdatedAt = now;
        await unitOfWork.SaveChangesAsync();

        return Result.Success(AccountantConstants.TransactionDeletedMessage);
    }
}
