using DentalDashboard.ApplicationService.Contract.Secretary.Accountant.SecretarySales;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.ApplicationService.Contract.Secretary.Accountant.SecretarySales.Queries;
using DentalDashboard.Domain.Secretary.Accountant.SecretarySales.Enums;
using DentalDashboard.Domain.Secretary.Accountant.SecretarySales.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Secretary.Accountant.SecretarySales.Handlers;

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
