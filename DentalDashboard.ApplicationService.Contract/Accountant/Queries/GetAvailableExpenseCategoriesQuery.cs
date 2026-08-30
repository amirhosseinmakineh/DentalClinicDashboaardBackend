using DentalDashboard.ApplicationService.Contract.Accountant.DTOs;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Domain;

namespace DentalDashboard.ApplicationService.Contract.Accountant.Queries;

public sealed class GetAvailableExpenseCategoriesQuery : IQuery<Result<IReadOnlyList<ExpenseCategoryDto>>>
{
}
