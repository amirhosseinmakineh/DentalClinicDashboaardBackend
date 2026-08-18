using DentalDashboard.Domain.Models;

namespace DentalDashboard.Domain.IRepositories;

public interface IFinancialTransactionRepository
{
    Task<FinancialTransaction?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<Wallet?> GetWalletByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<FinancialTransaction> AddAsync(FinancialTransaction transaction, CancellationToken cancellationToken = default);
    Task<Wallet> AddWalletTransactionAsync(Guid userId, FinancialTransaction transaction,
        WalletTransaction walletTransaction, CancellationToken cancellationToken = default);
}
