using DentalDashboard.Accounting.Contracts.SecretarySales;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Pagination;
using DentalDashboard.Accounting.Contracts.SecretarySales.Queries;
using DentalDashboard.Domain.Models;
using DentalDashboard.Accounting.Domain.SecretarySales.Enums;
using DentalDashboard.Accounting.Domain.SecretarySales.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Accounting.Application.SecretarySales.Handlers;

public sealed class GetSecretarySaleServicesQueryHandler(ISecretarySalesRepository repository)
    : IQueryHandler<GetSecretarySaleServicesQuery, PaginatedResult<SecretarySaleServiceDto>>
{
    public async Task<PaginatedResult<SecretarySaleServiceDto>> HandleAsync(
        GetSecretarySaleServicesQuery query,
        CancellationToken cancellationToken = default)
    {
        var source = repository.Services.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var searchTerm = query.Search.Trim();
            source = source.Where(service => service.Title.Contains(searchTerm));
        }

        if (query.IsActive.HasValue)
        {
            source = source.Where(service => service.IsActive == query.IsActive.Value);
        }

        return await source
            .OrderByDescending(service => service.CreatedAt)
            .Select(service => new SecretarySaleServiceDto(
                service.Id,
                service.Title,
                service.Price,
                service.SecretaryReward,
                service.IsActive,
                service.CreatedAt,
                service.UpdatedAt))
            .ToPaginatedResultAsync(query.Page, query.PageSize, cancellationToken);
    }
}
