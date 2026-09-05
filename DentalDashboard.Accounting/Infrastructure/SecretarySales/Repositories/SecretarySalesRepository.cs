using DentalDashboard.Domain.Models;
using DentalDashboard.Accounting.Domain.SecretarySales.Entities;
using DentalDashboard.Accounting.Domain.SecretarySales.IRepositories;
using DentalDashboard.Infrastracture.Context;

namespace DentalDashboard.Accounting.Infrastructure.SecretarySales.Repositories;

public sealed class SecretarySalesRepository(DentalContext context) : ISecretarySalesRepository
{
    public IQueryable<SecretarySaleService> Services => context.SecretarySaleServices;
    public IQueryable<SecretarySale> Sales => context.SecretarySales;
    public IQueryable<SecretaryWallet> Wallets => context.SecretaryWallets;
    public IQueryable<SecretaryWalletTransaction> WalletTransactions =>
        context.SecretaryWalletTransactions;
    public IQueryable<User> Users => context.Users;

    public Task AddServiceAsync(
        SecretarySaleService service,
        CancellationToken cancellationToken)
    {
        return context.SecretarySaleServices
            .AddAsync(service, cancellationToken)
            .AsTask();
    }

    public Task AddSaleAsync(
        SecretarySale sale,
        CancellationToken cancellationToken)
    {
        return context.SecretarySales.AddAsync(sale, cancellationToken).AsTask();
    }

    public Task AddWalletAsync(
        SecretaryWallet wallet,
        CancellationToken cancellationToken)
    {
        return context.SecretaryWallets.AddAsync(wallet, cancellationToken).AsTask();
    }

    public Task AddWalletTransactionAsync(
        SecretaryWalletTransaction transaction,
        CancellationToken cancellationToken)
    {
        return context.SecretaryWalletTransactions
            .AddAsync(transaction, cancellationToken)
            .AsTask();
    }
}
