using DentalDashboard.Domain.Secretary.Accountant.Entities;
using DentalDashboard.Domain.Secretary.Accountant.IRepositories;
using DentalDashboard.Infrastracture.Context;

namespace DentalDashboard.Infrastracture.Secretary.Accountant.Repositories;

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
