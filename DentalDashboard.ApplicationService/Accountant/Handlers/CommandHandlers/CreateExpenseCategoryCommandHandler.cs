using DentalDashboard.ApplicationService.Contract.Accountant.Commands;
using DentalDashboard.Domain.Accountant.Entities;
using DentalDashboard.Domain.Accountant.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Accountant.Handlers.CommandHandlers;

public sealed class CreateExpenseCategoryCommandHandler
    : ICommandHandler<CreateExpenseCommand, CreateExpenseResponse>
{
    private readonly IExpenseRepository repository;

    public CreateExpenseCategoryCommandHandler(IExpenseRepository repository)
    {
        this.repository = repository;
    }

    public async Task<Result<CreateExpenseResponse>> HandleAsync(
        CreateExpenseCommand command,
        CancellationToken cancellationToken = default)
    {
        var title = command.Title.Trim();
        var titleExists = await repository.GetAll()
            .AnyAsync(x => x.Title == title, cancellationToken);
        if (titleExists)
        {
            return Result<CreateExpenseResponse>.Failure(
                AccountantConstants.ExpenseCategoryDuplicateTitleMessage);
        }

        var category = new ExpenseCategory
        {
            Title = title,
            IsActive = command.IsActive
        };

        await repository.AddAsync(category);
        await repository.SaveChange();

        return Result<CreateExpenseResponse>.Success(
            new CreateExpenseResponse(category.Id, category.Title, category.IsActive),
            AccountantConstants.ExpenseCategoryCreatedMessage);
    }
}
