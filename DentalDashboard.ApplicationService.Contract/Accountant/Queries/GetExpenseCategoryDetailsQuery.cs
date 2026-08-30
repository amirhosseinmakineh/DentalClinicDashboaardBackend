using DentalDashboard.ApplicationService.Contract.Accountant.Commands;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Domain;

namespace DentalDashboard.ApplicationService.Contract.Accountant.Queries;

public sealed record GetExpenseCategoryDetailsQuery(long Id)
    : IQuery<Result<ExpenseCategoryResponse>?>;
