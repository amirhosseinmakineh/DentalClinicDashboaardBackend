using DentalDashboard.Domain.Models;
using DentalDashboard.Domain.Secretary.Accountant.SecretarySales.Entities;

namespace DentalDashboard.Domain.Secretary.Accountant.SecretarySales.IRepositories;

public interface ISecretarySalesRepository
{
    IQueryable<SecretarySaleService> Services { get; }
    IQueryable<SecretarySale> Sales { get; }
    IQueryable<SecretaryWallet> Wallets { get; }
    IQueryable<SecretaryWalletTransaction> WalletTransactions { get; }
    IQueryable<User> Users { get; }

    Task AddServiceAsync(SecretarySaleService entity, CancellationToken cancellationToken);
    Task AddSaleAsync(SecretarySale entity, CancellationToken cancellationToken);
    Task AddWalletAsync(SecretaryWallet entity, CancellationToken cancellationToken);
    Task AddWalletTransactionAsync(SecretaryWalletTransaction entity, CancellationToken cancellationToken);
}
