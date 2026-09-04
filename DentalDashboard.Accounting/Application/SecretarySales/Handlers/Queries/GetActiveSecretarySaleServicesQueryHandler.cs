using DentalDashboard.ApplicationService.Contract.Secretary.Accountant.SecretarySales;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.ApplicationService.Contract.Secretary.Accountant.SecretarySales.Queries;
using DentalDashboard.Domain.Secretary.Accountant.SecretarySales.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Secretary.Accountant.SecretarySales.Handlers;

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
