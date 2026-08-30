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
public sealed class GetAvailableExpenseCategoriesQueryHandler : IQueryHandler<GetAvailableExpenseCategoriesQuery, Result<IReadOnlyList<ExpenseCategoryDto>>>
{
    private readonly IAccountantRepository repository;
    public GetAvailableExpenseCategoriesQueryHandler(IAccountantRepository repository)
    {
        this.repository = repository;
    }
    public async Task<Result<IReadOnlyList<ExpenseCategoryDto>>> HandleAsync(GetAvailableExpenseCategoriesQuery query, CancellationToken cancellationToken = default)
    {
        var categories = await repository.ExpenseCategories
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.IsActive)
            .OrderBy(x => x.Title)
            .Select(x => new ExpenseCategoryDto(x.Id, x.Title))
            .ToListAsync(cancellationToken);
        return Result<IReadOnlyList<ExpenseCategoryDto>>.Success(categories);
    }
}
