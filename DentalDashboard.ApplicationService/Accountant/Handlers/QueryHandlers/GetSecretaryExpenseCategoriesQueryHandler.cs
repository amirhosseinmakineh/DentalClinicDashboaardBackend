using DentalDashboard.ApplicationService.Accountant;
using DentalDashboard.ApplicationService.Contract.Accountant.Commands;
using DentalDashboard.ApplicationService.Contract.Accountant.DTOs;
using DentalDashboard.ApplicationService.Contract.Accountant.Queries;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Accountant.Entities;
using DentalDashboard.Domain.Accountant.Enums;
using DentalDashboard.Domain.Accountant.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Accountant.Handlers.QueryHandlers;
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
