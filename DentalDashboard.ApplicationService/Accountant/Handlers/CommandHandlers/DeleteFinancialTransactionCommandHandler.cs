using DentalDashboard.ApplicationService.Contract.Accountant.Commands;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Accountant.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Accountant.Handlers.CommandHandlers;

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
