using DentalDashboard.Accounting.Contracts.SecretarySales;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Pagination;
using DentalDashboard.Accounting.Contracts.SecretarySales.Queries;
using DentalDashboard.Domain.Models;
using DentalDashboard.Accounting.Domain.SecretarySales.Enums;
using DentalDashboard.Accounting.Domain.SecretarySales.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Accounting.Application.SecretarySales.Handlers;

public sealed class GetSecretaryWalletTransactionsQueryHandler(
    ISecretarySalesRepository repository)
    : IQueryHandler<GetSecretaryWalletTransactionsQuery, PaginatedResult<SecretaryWalletTransactionDto>>
{
    public async Task<PaginatedResult<SecretaryWalletTransactionDto>> HandleAsync(
        GetSecretaryWalletTransactionsQuery query,
        CancellationToken cancellationToken = default)
    {
        var source = repository.WalletTransactions
            .AsNoTracking()
            .Where(transaction => transaction.SecretaryUserId == query.SecretaryUserId);

        if (query.TransactionType.HasValue)
        {
            source = source.Where(
                transaction => transaction.TransactionType == query.TransactionType.Value);
        }

        if (query.FromDate.HasValue)
        {
            source = source.Where(
                transaction => transaction.CreatedAt >= query.FromDate.Value);
        }

        if (query.ToDate.HasValue)
        {
            var exclusiveEndDate = query.ToDate.Value.Date.AddDays(1);
            source = source.Where(transaction => transaction.CreatedAt < exclusiveEndDate);
        }

        return await source
            .OrderByDescending(transaction => transaction.CreatedAt)
            .Select(transaction => new SecretaryWalletTransactionDto(
                transaction.Id,
                transaction.Amount,
                transaction.TransactionType,
                transaction.Description,
                transaction.CreatedAt,
                transaction.SecretarySaleId,
                transaction.SecretarySale == null
                    ? null
                    : transaction.SecretarySale.Service.Title,
                transaction.SecretarySale == null
                    ? null
                    : transaction.SecretarySale.PatientUser.FirstName + " " +
                      transaction.SecretarySale.PatientUser.LastName))
            .ToPaginatedResultAsync(query.Page, query.PageSize, cancellationToken);
    }
}
