using DentalDashboard.Accounting.Contracts.DTOs;
using DentalDashboard.Accounting.Contracts.Queries;
using DentalDashboard.Accounting.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Accounting.Application.Handlers.QueryHandlers;

public sealed class GetSecretaryExpenseCategoriesQueryHandler(
    ISecretaryAccountRepository repository)
    : IQueryHandler<
        GetSecretaryExpenseCategoriesQuery,
        Result<IReadOnlyList<SecretaryExpenseCategoryDto>>>
{
    public async Task<Result<IReadOnlyList<SecretaryExpenseCategoryDto>>> HandleAsync(
        GetSecretaryExpenseCategoriesQuery query,
        CancellationToken cancellationToken = default)
    {
        var categories = await repository.ExpenseCategories
            .AsNoTracking()
            .Where(item => !item.IsDeleted && item.IsActive)
            .OrderBy(item => item.Title)
            .Select(item => new SecretaryExpenseCategoryDto(item.Id, item.Title))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<SecretaryExpenseCategoryDto>>.Success(categories);
    }
}
