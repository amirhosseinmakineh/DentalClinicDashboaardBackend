using DentalDashboard.ApplicationService.Secretary.Account;
using DentalDashboard.ApplicationService.Contract.Secretary.Account.Commands;
using DentalDashboard.ApplicationService.Contract.Secretary.Account.DTOs;
using DentalDashboard.ApplicationService.Contract.Secretary.Account.Queries;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Secretary.Account.Entities;
using DentalDashboard.Domain.Secretary.Account.Enums;
using DentalDashboard.Domain.Secretary.Account.Repositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Secretary.Account.Handlers.QueryHandlers;
public sealed class GetSecretaryExpenseCategoriesQueryHandler : IQueryHandler<GetSecretaryExpenseCategoriesQuery, Result<IReadOnlyList<SecretaryExpenseCategoryDto>>>
{
    private readonly ISecretaryAccountRepository repository;
    public GetSecretaryExpenseCategoriesQueryHandler(ISecretaryAccountRepository repository)
    {
        this.repository = repository;
    }
    public async Task<Result<IReadOnlyList<SecretaryExpenseCategoryDto>>> HandleAsync(GetSecretaryExpenseCategoriesQuery query, CancellationToken cancellationToken = default)
    {
        var categories = await repository.ExpenseCategories
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.IsActive)
            .OrderBy(x => x.Title)
            .Select(x => new SecretaryExpenseCategoryDto(x.Id, x.Title))
            .ToListAsync(cancellationToken);
        return Result<IReadOnlyList<SecretaryExpenseCategoryDto>>.Success(categories);
    }
}
