using DentalDashboard.Accounting.Contracts.DTOs;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Domain;

namespace DentalDashboard.Accounting.Contracts.Queries;

public sealed class GetSecretaryExpenseCategoriesQuery : IQuery<Result<IReadOnlyList<SecretaryExpenseCategoryDto>>>
{
}
