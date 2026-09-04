using DentalDashboard.ApplicationService.Contract.Secretary.Accountant.SecretarySales;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Pagination;
using DentalDashboard.ApplicationService.Contract.Secretary.Accountant.SecretarySales.Queries;
using DentalDashboard.Domain.Models;
using DentalDashboard.Domain.Secretary.Accountant.SecretarySales.IRepositories;

namespace DentalDashboard.ApplicationService.Secretary.Accountant.SecretarySales.Handlers;

public sealed class GetSecretarySalesQueryHandler(ISecretarySalesRepository repository)
    : IQueryHandler<GetSecretarySalesQuery, PaginatedResult<SecretarySaleDto>>
{
    public Task<PaginatedResult<SecretarySaleDto>> HandleAsync(
        GetSecretarySalesQuery query,
        CancellationToken cancellationToken = default)
    {
        return SaleQuery.Apply(
                repository,
                query.Search,
                query.SecretaryUserId,
                null,
                query.ServiceId,
                query.Status,
                query.FromDate,
                query.ToDate)
            .ToPaginatedResultAsync(query.Page, query.PageSize, cancellationToken);
    }
}
