using DentalDashboard.ApplicationService.Contract.Secretary.Account.DTOs;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Domain;

namespace DentalDashboard.ApplicationService.Contract.Secretary.Account.Queries;

public sealed class GetSecretaryExpenseCategoriesQuery : IQuery<Result<IReadOnlyList<SecretaryExpenseCategoryDto>>>
{
}
