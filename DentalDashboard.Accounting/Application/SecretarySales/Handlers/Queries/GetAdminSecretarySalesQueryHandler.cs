using DentalDashboard.Accounting.Contracts.SecretarySales;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Pagination;
using DentalDashboard.Accounting.Contracts.SecretarySales.Queries;
using DentalDashboard.Domain.Models;
using DentalDashboard.Accounting.Domain.SecretarySales.IRepositories;

namespace DentalDashboard.Accounting.Application.SecretarySales.Handlers;

public sealed class GetAdminSecretarySalesQueryHandler(ISecretarySalesRepository repository)
    : IQueryHandler<GetAdminSecretarySalesQuery, PaginatedResult<SecretarySaleDto>>
{
    public Task<PaginatedResult<SecretarySaleDto>> HandleAsync(
        GetAdminSecretarySalesQuery query,
        CancellationToken cancellationToken = default)
    {
        return SaleQuery.Apply(
                repository,
                query.Search,
                query.SecretaryUserId,
                query.PatientUserId,
                query.ServiceId,
                query.Status,
                query.FromDate,
                query.ToDate)
            .ToPaginatedResultAsync(query.Page, query.PageSize, cancellationToken);
    }
}
