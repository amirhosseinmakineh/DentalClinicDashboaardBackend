using DentalDashboard.Accounting.Contracts.Commands;
using DentalDashboard.Accounting.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Accounting.Application.Handlers.CommandHandlers;

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
            .FirstOrDefaultAsync(item => item.Id == command.Id && !item.IsDeleted, cancellationToken);
        if (category is null)
        {
            return Result<ExpenseCategoryResponse>.Failure(
                SecretaryAccountConstants.ExpenseCategoryNotFoundMessage);
        }

        var title = command.Title.Trim();
        var titleExists = await repository.GetAll()
            .AnyAsync(item => item.Id != command.Id && item.Title == title, cancellationToken);
        if (titleExists)
        {
            return Result<ExpenseCategoryResponse>.Failure(
                SecretaryAccountConstants.ExpenseCategoryDuplicateTitleMessage);
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
            SecretaryAccountConstants.ExpenseCategoryUpdatedMessage);
    }
}
