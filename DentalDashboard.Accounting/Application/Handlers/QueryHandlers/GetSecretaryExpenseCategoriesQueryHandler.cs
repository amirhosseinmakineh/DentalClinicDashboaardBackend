using DentalDashboard.ApplicationService.Contract.Secretary.Accountant.DTOs;
using DentalDashboard.ApplicationService.Contract.Secretary.Accountant.Queries;
using DentalDashboard.Domain.Secretary.Accountant.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Secretary.Accountant.Handlers.QueryHandlers;

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
