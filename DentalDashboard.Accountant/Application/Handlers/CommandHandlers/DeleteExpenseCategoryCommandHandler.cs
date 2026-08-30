using DentalDashboard.Accountant.Application.Contracts.Commands;
using DentalDashboard.Accountant.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Accountant.Application.Handlers.CommandHandlers;

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
            return Result.Failure(AccountantConstants.ExpenseCategoryNotFoundMessage);
        }

        category.IsDeleted = true;
        category.IsActive = false;
        category.DeletedAt = DateTime.UtcNow;
        category.UpdatedAt = DateTime.UtcNow;
        repository.Update(category);
        await repository.SaveChange();

        return Result.Success(AccountantConstants.ExpenseCategoryDeletedMessage);
    }
}
