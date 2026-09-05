using DentalDashboard.Accounting.Contracts.SecretarySales;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Accounting.Contracts.SecretarySales.Queries;
using DentalDashboard.Accounting.Domain.SecretarySales.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Accounting.Application.SecretarySales.Handlers;

public sealed class GetActiveSecretarySaleServicesQueryHandler(
    ISecretarySalesRepository repository)
    : IQueryHandler<GetActiveSecretarySaleServicesQuery, IReadOnlyList<SecretarySaleServiceDto>>
{
    public async Task<IReadOnlyList<SecretarySaleServiceDto>> HandleAsync(
        GetActiveSecretarySaleServicesQuery query,
        CancellationToken cancellationToken = default)
    {
        return await repository.Services
            .AsNoTracking()
            .Where(service => service.IsActive)
            .OrderBy(service => service.Title)
            .Select(service => new SecretarySaleServiceDto(
                service.Id,
                service.Title,
                service.Price,
                service.SecretaryReward,
                service.IsActive,
                service.CreatedAt,
                service.UpdatedAt))
            .ToListAsync(cancellationToken);
    }
}
