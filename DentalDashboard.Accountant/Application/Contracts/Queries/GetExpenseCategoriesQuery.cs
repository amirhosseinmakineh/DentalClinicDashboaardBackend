using DentalDashboard.Accountant.Application.Contracts.Commands;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Domain;

namespace DentalDashboard.Accountant.Application.Contracts.Queries;

public sealed record GetExpenseCategoriesQuery
    : IQuery<Result<IReadOnlyList<ExpenseCategoryResponse>>>;
