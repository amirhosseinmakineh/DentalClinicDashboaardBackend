using DentalDashboard.Domain.Models;
using DentalDashboard.Domain.Secretary.Accountant.SecretarySales.Entities;
using DentalDashboard.Domain.Secretary.Accountant.SecretarySales.IRepositories;
using DentalDashboard.Infrastracture.Context;

namespace DentalDashboard.Infrastracture.Secretary.Accountant.SecretarySales.Repositories;

public sealed class SecretarySalesRepository(DentalContext context) : ISecretarySalesRepository
{
    public IQueryable<SecretarySaleService> Services => context.SecretarySaleServices;
    public IQueryable<SecretarySale> Sales => context.SecretarySales;
    public IQueryable<SecretaryWallet> Wallets => context.SecretaryWallets;
    public IQueryable<SecretaryWalletTransaction> WalletTransactions => context.SecretaryWalletTransactions;
    public IQueryable<User> Users => context.Users;

    public Task AddServiceAsync(SecretarySaleService entity, CancellationToken cancellationToken) =>
        context.SecretarySaleServices.AddAsync(entity, cancellationToken).AsTask();

    public Task AddSaleAsync(SecretarySale entity, CancellationToken cancellationToken) =>
        context.SecretarySales.AddAsync(entity, cancellationToken).AsTask();

    public Task AddWalletAsync(SecretaryWallet entity, CancellationToken cancellationToken) =>
        context.SecretaryWallets.AddAsync(entity, cancellationToken).AsTask();

    public Task AddWalletTransactionAsync(SecretaryWalletTransaction entity, CancellationToken cancellationToken) =>
        context.SecretaryWalletTransactions.AddAsync(entity, cancellationToken).AsTask();
}
