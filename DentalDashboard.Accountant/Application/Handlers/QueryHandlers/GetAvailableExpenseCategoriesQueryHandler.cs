using DentalDashboard.Accountant.Application;
using DentalDashboard.Accountant.Application.Contracts.Commands;
using DentalDashboard.Accountant.Application.Contracts.DTOs;
using DentalDashboard.Accountant.Application.Contracts.Queries;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Accountant.Domain.Entities;
using DentalDashboard.Accountant.Domain.Enums;
using DentalDashboard.Accountant.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Accountant.Application.Handlers.QueryHandlers;
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
