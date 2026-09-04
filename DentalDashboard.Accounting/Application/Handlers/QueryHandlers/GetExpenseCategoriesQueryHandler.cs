using DentalDashboard.ApplicationService.Contract.Secretary.Accountant.Commands;
using DentalDashboard.ApplicationService.Contract.Secretary.Accountant.Queries;
using DentalDashboard.Domain.Secretary.Accountant.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Secretary.Accountant.Handlers.QueryHandlers;

public sealed class GetExpenseCategoriesQueryHandler
    : IQueryHandler<GetExpenseCategoriesQuery, Result<IReadOnlyList<ExpenseCategoryResponse>>>
{
    private readonly IExpenseRepository repository;

    public GetExpenseCategoriesQueryHandler(IExpenseRepository repository)
    {
        this.repository = repository;
    }

    public async Task<Result<IReadOnlyList<ExpenseCategoryResponse>>> HandleAsync(
        GetExpenseCategoriesQuery query,
        CancellationToken cancellationToken = default)
    {
        var categories = await repository.GetAll()
            .AsNoTracking()
            .Where(item => !item.IsDeleted)
            .OrderBy(item => item.Title)
            .Select(item => new ExpenseCategoryResponse(
                item.Id,
                item.Title,
                item.IsActive,
                item.CreatedAt,
                item.UpdatedAt))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<ExpenseCategoryResponse>>.Success(categories);
    }
}
