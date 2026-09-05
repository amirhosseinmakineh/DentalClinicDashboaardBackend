using DentalDashboard.Accounting.Contracts.Commands;
using DentalDashboard.Accounting.Contracts.Queries;
using DentalDashboard.Accounting.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Accounting.Application.Handlers.QueryHandlers;

public sealed class GetExpenseCategoryDetailsQueryHandler
    : IQueryHandler<GetExpenseCategoryDetailsQuery, Result<ExpenseCategoryResponse>?>
{
    private readonly IExpenseRepository repository;

    public GetExpenseCategoryDetailsQueryHandler(IExpenseRepository repository)
    {
        this.repository = repository;
    }

    public async Task<Result<ExpenseCategoryResponse>?> HandleAsync(
        GetExpenseCategoryDetailsQuery query,
        CancellationToken cancellationToken = default)
    {
        var category = await repository.GetAll()
            .AsNoTracking()
            .Where(item => item.Id == query.Id && !item.IsDeleted)
            .Select(item => new ExpenseCategoryResponse(
                item.Id,
                item.Title,
                item.IsActive,
                item.CreatedAt,
                item.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        return category is null
            ? null
            : Result<ExpenseCategoryResponse>.Success(category);
    }
}
