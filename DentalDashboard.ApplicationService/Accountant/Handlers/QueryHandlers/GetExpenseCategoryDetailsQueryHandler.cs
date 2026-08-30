using DentalDashboard.ApplicationService.Contract.Accountant.Commands;
using DentalDashboard.ApplicationService.Contract.Accountant.Queries;
using DentalDashboard.Domain.Accountant.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Accountant.Handlers.QueryHandlers;

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
            .Where(x => x.Id == query.Id && !x.IsDeleted)
            .Select(x => new ExpenseCategoryResponse(
                x.Id,
                x.Title,
                x.IsActive,
                x.CreatedAt,
                x.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        return category is null
            ? null
            : Result<ExpenseCategoryResponse>.Success(category);
    }
}
