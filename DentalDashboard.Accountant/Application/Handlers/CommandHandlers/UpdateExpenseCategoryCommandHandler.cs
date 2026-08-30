using DentalDashboard.Accountant.Application.Contracts.Commands;
using DentalDashboard.Accountant.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Accountant.Application.Handlers.CommandHandlers;

public sealed class UpdateExpenseCategoryCommandHandler
    : ICommandHandler<UpdateExpenseCategoryCommand, ExpenseCategoryResponse>
{
    private readonly IExpenseRepository repository;

    public UpdateExpenseCategoryCommandHandler(IExpenseRepository repository)
    {
        this.repository = repository;
    }

    public async Task<Result<ExpenseCategoryResponse>> HandleAsync(
        UpdateExpenseCategoryCommand command,
        CancellationToken cancellationToken = default)
    {
        var category = await repository.GetAll()
            .FirstOrDefaultAsync(x => x.Id == command.Id && !x.IsDeleted, cancellationToken);
        if (category is null)
        {
            return Result<ExpenseCategoryResponse>.Failure(
                AccountantConstants.ExpenseCategoryNotFoundMessage);
        }

        var title = command.Title.Trim();
        var titleExists = await repository.GetAll()
            .AnyAsync(x => x.Id != command.Id && x.Title == title, cancellationToken);
        if (titleExists)
        {
            return Result<ExpenseCategoryResponse>.Failure(
                AccountantConstants.ExpenseCategoryDuplicateTitleMessage);
        }

        category.Title = title;
        category.IsActive = command.IsActive;
        category.UpdatedAt = DateTime.UtcNow;
        repository.Update(category);
        await repository.SaveChange();

        return Result<ExpenseCategoryResponse>.Success(
            new ExpenseCategoryResponse(
                category.Id,
                category.Title,
                category.IsActive,
                category.CreatedAt,
                category.UpdatedAt),
            AccountantConstants.ExpenseCategoryUpdatedMessage);
    }
}
