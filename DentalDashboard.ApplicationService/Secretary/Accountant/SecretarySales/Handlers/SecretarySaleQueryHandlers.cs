using DentalDashboard.ApplicationService.Contract.Secretary.Accountant.SecretarySales;
using DentalDashboard.ApplicationService.Contract.Secretary.Accountant.SecretarySales.Queries;
using DentalDashboard.Domain.Models;
using DentalDashboard.Domain.Secretary.Accountant.SecretarySales.Enums;
using DentalDashboard.Domain.Secretary.Accountant.SecretarySales.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Secretary.Accountant.SecretarySales.Handlers;

public sealed class GetSecretarySaleServicesQueryHandler(ISecretarySalesRepository repository)
    : IQueryHandler<GetSecretarySaleServicesQuery, PaginatedResult<SecretarySaleServiceDto>>
{
    public async Task<PaginatedResult<SecretarySaleServiceDto>> HandleAsync(GetSecretarySaleServicesQuery query, CancellationToken cancellationToken = default)
    {
        var source = repository.Services.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Search)) source = source.Where(x => x.Title.Contains(query.Search.Trim()));
        if (query.IsActive.HasValue) source = source.Where(x => x.IsActive == query.IsActive.Value);
        return await source.OrderByDescending(x => x.CreatedAt).Select(x => new SecretarySaleServiceDto(x.Id, x.Title, x.Price, x.SecretaryReward, x.IsActive, x.CreatedAt, x.UpdatedAt)).Page(query.Page, query.PageSize, cancellationToken);
    }
}

public sealed class GetActiveSecretarySaleServicesQueryHandler(ISecretarySalesRepository repository)
    : IQueryHandler<GetActiveSecretarySaleServicesQuery, IReadOnlyList<SecretarySaleServiceDto>>
{
    public async Task<IReadOnlyList<SecretarySaleServiceDto>> HandleAsync(GetActiveSecretarySaleServicesQuery query, CancellationToken cancellationToken = default) =>
        await repository.Services.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Title)
            .Select(x => new SecretarySaleServiceDto(x.Id, x.Title, x.Price, x.SecretaryReward, x.IsActive, x.CreatedAt, x.UpdatedAt)).ToListAsync(cancellationToken);
}

public sealed class SearchSecretarySalePatientsQueryHandler(ISecretarySalesRepository repository)
    : IQueryHandler<SearchSecretarySalePatientsQuery, PaginatedResult<SecretarySalePatientDto>>
{
    public async Task<PaginatedResult<SecretarySalePatientDto>> HandleAsync(SearchSecretarySalePatientsQuery query, CancellationToken cancellationToken = default)
    {
        var source = repository.Users.AsNoTracking().Where(user => user.IsActive && !user.IsDeleted && user.UserRoles.Any(role => !role.IsDeleted && role.Role.RoleName == "Patient"));
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(x => x.FirstName.Contains(search) || x.LastName.Contains(search) || x.PhoneNumber.Contains(search) || (x.FirstName + " " + x.LastName).Contains(search));
        }
        return await source.OrderBy(x => x.LastName).ThenBy(x => x.FirstName)
            .Select(x => new SecretarySalePatientDto(x.Id, x.FirstName, x.LastName, x.PhoneNumber)).Page(query.Page, query.PageSize, cancellationToken);
    }
}

public sealed class GetAdminSecretarySalesQueryHandler(ISecretarySalesRepository repository)
    : IQueryHandler<GetAdminSecretarySalesQuery, PaginatedResult<SecretarySaleDto>>
{
    public Task<PaginatedResult<SecretarySaleDto>> HandleAsync(GetAdminSecretarySalesQuery query, CancellationToken cancellationToken = default) =>
        SaleQuery.Apply(repository, query.Search, query.SecretaryUserId, query.PatientUserId, query.ServiceId, query.Status, query.FromDate, query.ToDate)
            .Page(query.Page, query.PageSize, cancellationToken);
}

public sealed class GetSecretarySalesQueryHandler(ISecretarySalesRepository repository)
    : IQueryHandler<GetSecretarySalesQuery, PaginatedResult<SecretarySaleDto>>
{
    public Task<PaginatedResult<SecretarySaleDto>> HandleAsync(GetSecretarySalesQuery query, CancellationToken cancellationToken = default) =>
        SaleQuery.Apply(repository, query.Search, query.SecretaryUserId, null, query.ServiceId, query.Status, query.FromDate, query.ToDate)
            .Page(query.Page, query.PageSize, cancellationToken);
}

public sealed class GetSecretaryWalletQueryHandler(ISecretarySalesRepository repository)
    : IQueryHandler<GetSecretaryWalletQuery, SecretaryWalletDto>
{
    public async Task<SecretaryWalletDto> HandleAsync(GetSecretaryWalletQuery query, CancellationToken cancellationToken = default)
    {
        var wallet = await repository.Wallets.AsNoTracking().Where(x => x.SecretaryUserId == query.SecretaryUserId)
            .Select(x => new SecretaryWalletDto(x.Balance,
                x.Transactions.Where(t => t.TransactionType == SecretaryWalletTransactionType.SaleReward).Sum(t => (decimal?)t.Amount) ?? 0,
                x.Transactions.Count(t => t.TransactionType == SecretaryWalletTransactionType.SaleReward)))
            .FirstOrDefaultAsync(cancellationToken);
        return wallet ?? new SecretaryWalletDto(0, 0, 0);
    }
}

public sealed class GetSecretaryWalletTransactionsQueryHandler(ISecretarySalesRepository repository)
    : IQueryHandler<GetSecretaryWalletTransactionsQuery, PaginatedResult<SecretaryWalletTransactionDto>>
{
    public async Task<PaginatedResult<SecretaryWalletTransactionDto>> HandleAsync(GetSecretaryWalletTransactionsQuery query, CancellationToken cancellationToken = default)
    {
        var source = repository.WalletTransactions.AsNoTracking().Where(x => x.SecretaryUserId == query.SecretaryUserId);
        if (query.TransactionType.HasValue) source = source.Where(x => x.TransactionType == query.TransactionType.Value);
        if (query.FromDate.HasValue) source = source.Where(x => x.CreatedAt >= query.FromDate.Value);
        if (query.ToDate.HasValue) { var until = query.ToDate.Value.Date.AddDays(1); source = source.Where(x => x.CreatedAt < until); }
        return await source.OrderByDescending(x => x.CreatedAt).Select(x => new SecretaryWalletTransactionDto(
            x.Id, x.Amount, x.TransactionType, x.Description, x.CreatedAt, x.SecretarySaleId,
            x.SecretarySale == null ? null : x.SecretarySale.Service.Title,
            x.SecretarySale == null ? null : x.SecretarySale.PatientUser.FirstName + " " + x.SecretarySale.PatientUser.LastName))
            .Page(query.Page, query.PageSize, cancellationToken);
    }
}

internal static class SaleQuery
{
    public static IQueryable<SecretarySaleDto> Apply(ISecretarySalesRepository repository, string? search, Guid? secretaryId, Guid? patientId, long? serviceId, SecretarySaleStatus? status, DateTime? from, DateTime? to)
    {
        var source = repository.Sales.AsNoTracking();
        if (secretaryId.HasValue) source = source.Where(x => x.SecretaryUserId == secretaryId.Value);
        if (patientId.HasValue) source = source.Where(x => x.PatientUserId == patientId.Value);
        if (serviceId.HasValue) source = source.Where(x => x.ServiceId == serviceId.Value);
        if (status.HasValue) source = source.Where(x => x.Status == status.Value);
        if (from.HasValue) source = source.Where(x => x.CreatedAt >= from.Value);
        if (to.HasValue) { var until = to.Value.Date.AddDays(1); source = source.Where(x => x.CreatedAt < until); }
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            source = source.Where(x => x.SecretaryUser.FirstName.Contains(term) || x.SecretaryUser.LastName.Contains(term) || x.PatientUser.FirstName.Contains(term) || x.PatientUser.LastName.Contains(term) || x.PatientUser.PhoneNumber.Contains(term) || x.Service.Title.Contains(term));
        }
        return source.OrderByDescending(x => x.CreatedAt).Select(x => new SecretarySaleDto(x.Id, x.SecretaryUserId,
            x.SecretaryUser.FirstName + " " + x.SecretaryUser.LastName, x.PatientUserId,
            x.PatientUser.FirstName + " " + x.PatientUser.LastName, x.PatientUser.PhoneNumber,
            x.ServiceId, x.Service.Title, x.SalePrice, x.SecretaryReward, x.Status, x.CreatedAt, x.ReviewedAt));
    }
}

internal static class PaginationExtensions
{
    public static async Task<PaginatedResult<T>> Page<T>(this IQueryable<T> source, int page, int pageSize, CancellationToken cancellationToken)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var totalCount = await source.CountAsync(cancellationToken);
        var items = await source.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new PaginatedResult<T> { Items = items, TotalCount = totalCount, PageNumber = page, PageSize = pageSize };
    }
}
