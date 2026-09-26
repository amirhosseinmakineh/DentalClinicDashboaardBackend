using DentalDashboard.ApplicationService.Contract.Secretary.Accountant.Commands;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Secretary.Accountant.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Secretary.Accountant.Handlers.CommandHandlers;

public sealed class DeleteSecretaryFinancialTransactionCommandHandler
    : ICommandHandler<DeleteSecretaryFinancialTransactionCommand>
{
    private readonly ISecretaryAccountRepository repository;
    private readonly IUnitOfWork unitOfWork;

    public DeleteSecretaryFinancialTransactionCommandHandler(
        ISecretaryAccountRepository repository,
        IUnitOfWork unitOfWork)
    {
        this.repository = repository;
        this.unitOfWork = unitOfWork;
    }

    public async Task<Result> HandleAsync(
        DeleteSecretaryFinancialTransactionCommand command,
        CancellationToken cancellationToken = default)
    {
        var transaction = await repository.FinancialTransactions
            .FirstOrDefaultAsync(x => x.Id == command.Id && !x.IsDeleted, cancellationToken);
        if (transaction is null)
        {
            return Result.Failure(SecretaryAccountConstants.TransactionNotFoundMessage);
        }

        var now = DateTime.UtcNow;
        transaction.IsDeleted = true;
        transaction.DeletedAt = now;
        transaction.UpdatedAt = now;
        await unitOfWork.SaveChangesAsync();

        return Result.Success(SecretaryAccountConstants.TransactionDeletedMessage);
    }
}
