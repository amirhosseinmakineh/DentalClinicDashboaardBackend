using DentalDashboard.ApplicationService.Contract.Secretary.Account.Commands;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Domain;

namespace DentalDashboard.ApplicationService.Contract.Secretary.Account.Queries;

public sealed record GetExpenseCategoriesQuery
    : IQuery<Result<IReadOnlyList<ExpenseCategoryResponse>>>;
