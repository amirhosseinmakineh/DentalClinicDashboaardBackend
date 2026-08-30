using DentalDashboard.ApplicationService.Contract.Secretary.Account.Commands;
using DentalDashboard.Domain.Secretary.Account.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Secretary.Account.Handlers.CommandHandlers;

public sealed class DeleteExpenseCategoryCommandHandler
    : ICommandHandler<DeleteExpenseCategoryCommand>
{
    private readonly IExpenseRepository repository;

    public DeleteExpenseCategoryCommandHandler(IExpenseRepository repository)
    {
        this.repository = repository;
    }

    public async Task<Result> HandleAsync(
        DeleteExpenseCategoryCommand command,
        CancellationToken cancellationToken = default)
    {
        var category = await repository.GetAll()
            .FirstOrDefaultAsync(x => x.Id == command.Id && !x.IsDeleted, cancellationToken);
        if (category is null)
        {
            return Result.Failure(SecretaryAccountConstants.ExpenseCategoryNotFoundMessage);
        }

        category.IsDeleted = true;
        category.IsActive = false;
        category.DeletedAt = DateTime.UtcNow;
        category.UpdatedAt = DateTime.UtcNow;
        repository.Update(category);
        await repository.SaveChange();

        return Result.Success(SecretaryAccountConstants.ExpenseCategoryDeletedMessage);
    }
}
