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

    public Task<bool> UserHasRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken = default) =>
        context.UserRoles.AnyAsync(x => x.UserId == userId && !x.IsDeleted && x.Role != null &&
            !x.Role.IsDeleted && x.Role.RoleName == roleName, cancellationToken);

    public Task<FinancialTransaction?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        context.FinancialTransactions.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<(Wallet Wallet, int TotalCount)> GetOrCreateWalletByUserIdAsync(Guid userId, int page,
        int pageSize, CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var wallet = await context.Wallets.SingleOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        if (wallet is null)
        {
            wallet = new Wallet { UserId = userId };
            context.Wallets.Add(wallet);
            await context.SaveChangesAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);

        var query = context.WalletTransactions.AsNoTracking().Where(x => x.WalletId == wallet.Id);
        var totalCount = await query.CountAsync(cancellationToken);
        wallet.Transactions = await query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (wallet, totalCount);
    }

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
        var wallet = await context.Wallets.SingleOrDefaultAsync(x => x.UserId == userId, cancellationToken)
            ?? throw new InvalidOperationException("Wallet does not exist.");
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
        return (await context.Wallets.AsNoTracking().SingleAsync(x => x.Id == wallet.Id, cancellationToken));
    }
}
