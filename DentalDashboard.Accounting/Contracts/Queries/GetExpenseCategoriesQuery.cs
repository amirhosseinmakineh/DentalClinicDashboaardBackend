using DentalDashboard.Accounting.Contracts.Commands;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Domain;

namespace DentalDashboard.Accounting.Contracts.Queries;

public sealed record GetExpenseCategoriesQuery
    : IQuery<Result<IReadOnlyList<ExpenseCategoryResponse>>>;
