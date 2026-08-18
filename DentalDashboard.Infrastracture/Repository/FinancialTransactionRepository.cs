using System.Data;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Models;
using DentalDashboard.Infrastracture.Context;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Infrastracture.Repository;

public class FinancialTransactionRepository : IFinancialTransactionRepository
{
    private readonly DentalContext context;
    public FinancialTransactionRepository(DentalContext context) => this.context = context;

    public Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken = default) =>
        context.Users.AnyAsync(x => x.Id == userId && !x.IsDeleted && x.IsActive, cancellationToken);

    public Task<FinancialTransaction?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        context.FinancialTransactions.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<Wallet?> GetWalletByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        context.Wallets.AsNoTracking().Include(x => x.Transactions.OrderByDescending(t => t.CreatedAt))
            .SingleOrDefaultAsync(x => x.UserId == userId, cancellationToken);

    public async Task<FinancialTransaction> AddAsync(FinancialTransaction transaction, CancellationToken cancellationToken = default)
    {
        context.FinancialTransactions.Add(transaction);
        await context.SaveChangesAsync(cancellationToken);
        return transaction;
    }

    public async Task<Wallet> AddWalletTransactionAsync(Guid userId, FinancialTransaction transaction,
        WalletTransaction walletTransaction, CancellationToken cancellationToken = default)
    {
        await using var dbTransaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var wallet = await context.Wallets.Include(x => x.Transactions)
            .SingleOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (wallet is null)
        {
            wallet = new Wallet { UserId = userId };
            context.Wallets.Add(wallet);
            await context.SaveChangesAsync(cancellationToken);
        }
        if (!wallet.IsActive)
            throw new InvalidOperationException("Wallet is inactive.");
        if (walletTransaction.Type == WalletTransactionType.Withdrawal && wallet.Balance < walletTransaction.Amount)
            throw new InvalidOperationException("Withdrawal amount cannot exceed wallet balance.");

        walletTransaction.WalletId = wallet.Id;
        walletTransaction.FinancialTransaction = transaction;
        context.WalletTransactions.Add(walletTransaction);
        var delta = walletTransaction.Type == WalletTransactionType.Deposit ? walletTransaction.Amount : -walletTransaction.Amount;
        await context.Wallets.Where(x => x.Id == wallet.Id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Balance, x => x.Balance + delta), cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await dbTransaction.CommitAsync(cancellationToken);
        context.ChangeTracker.Clear();
        return (await GetWalletByUserIdAsync(userId, cancellationToken))!;
    }
}
