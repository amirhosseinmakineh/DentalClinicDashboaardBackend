using DentalDashboard.Accountant.Application.Contracts.DTOs;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Domain;

namespace DentalDashboard.Accountant.Application.Contracts.Queries;

public sealed class GetAvailableExpenseCategoriesQuery : IQuery<Result<IReadOnlyList<ExpenseCategoryDto>>>
{
}
