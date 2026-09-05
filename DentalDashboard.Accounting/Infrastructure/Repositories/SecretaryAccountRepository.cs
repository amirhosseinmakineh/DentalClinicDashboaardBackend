using DentalDashboard.Accounting.Domain.Entities;
using DentalDashboard.Accounting.Domain.IRepositories;
using DentalDashboard.Infrastracture.Context;

namespace DentalDashboard.Accounting.Infrastructure.Repositories;

public sealed class SecretaryAccountRepository : ISecretaryAccountRepository
{
    private readonly DentalContext context;

    public SecretaryAccountRepository(DentalContext context)
    {
        this.context = context;
    }

    public IQueryable<FinancialTransaction> FinancialTransactions => context.FinancialTransactions;
    public IQueryable<ExpenseCategory> ExpenseCategories => context.ExpenseCategories;

    public async Task AddTransactionAsync(
        FinancialTransaction transaction,
        CancellationToken cancellationToken)
    {
        await context.FinancialTransactions.AddAsync(transaction, cancellationToken);
    }
}
