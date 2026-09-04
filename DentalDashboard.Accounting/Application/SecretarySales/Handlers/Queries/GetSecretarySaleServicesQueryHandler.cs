using DentalDashboard.ApplicationService.Contract.Secretary.Accountant.SecretarySales;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Pagination;
using DentalDashboard.ApplicationService.Contract.Secretary.Accountant.SecretarySales.Queries;
using DentalDashboard.Domain.Models;
using DentalDashboard.Domain.Secretary.Accountant.SecretarySales.Enums;
using DentalDashboard.Domain.Secretary.Accountant.SecretarySales.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Secretary.Accountant.SecretarySales.Handlers;

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
