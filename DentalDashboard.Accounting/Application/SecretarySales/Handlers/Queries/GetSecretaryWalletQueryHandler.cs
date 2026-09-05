using DentalDashboard.Accounting.Contracts.SecretarySales;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Accounting.Contracts.SecretarySales.Queries;
using DentalDashboard.Accounting.Domain.SecretarySales.Enums;
using DentalDashboard.Accounting.Domain.SecretarySales.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Accounting.Application.SecretarySales.Handlers;

public sealed class GetSecretaryWalletQueryHandler(ISecretarySalesRepository repository)
    : IQueryHandler<GetSecretaryWalletQuery, SecretaryWalletDto>
{
    public async Task<SecretaryWalletDto> HandleAsync(
        GetSecretaryWalletQuery query,
        CancellationToken cancellationToken = default)
    {
        var wallet = await repository.Wallets
            .AsNoTracking()
            .Where(item => item.SecretaryUserId == query.SecretaryUserId)
            .Select(item => new SecretaryWalletDto(
                item.Balance,
                item.Transactions
                    .Where(transaction =>
                        transaction.TransactionType == SecretaryWalletTransactionType.SaleReward)
                    .Sum(transaction => (decimal?)transaction.Amount) ?? 0,
                item.Transactions.Count(transaction =>
                    transaction.TransactionType == SecretaryWalletTransactionType.SaleReward)))
            .FirstOrDefaultAsync(cancellationToken);

        return wallet ?? new SecretaryWalletDto(0, 0, 0);
    }
}
