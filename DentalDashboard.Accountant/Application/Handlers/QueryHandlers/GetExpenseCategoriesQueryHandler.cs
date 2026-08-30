using DentalDashboard.Accountant.Application.Contracts.Commands;
using DentalDashboard.Accountant.Application.Contracts.Queries;
using DentalDashboard.Accountant.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Accountant.Application.Handlers.QueryHandlers;

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
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Title)
            .Select(x => new ExpenseCategoryResponse(
                x.Id,
                x.Title,
                x.IsActive,
                x.CreatedAt,
                x.UpdatedAt))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<ExpenseCategoryResponse>>.Success(categories);
    }
}
