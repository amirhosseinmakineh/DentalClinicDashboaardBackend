using DentalDashboard.Accountant.Domain.Entities;
using DentalDashboard.Accountant.Domain.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Accountant.Infrastructure.Repositories;

public sealed class AccountantRepository : IAccountantRepository
{
    private readonly DbContext context;

    public AccountantRepository(DbContext context)
    {
        this.context = context;
    }

    public IQueryable<FinancialTransaction> FinancialTransactions => context.Set<FinancialTransaction>();
    public IQueryable<ExpenseCategory> ExpenseCategories => context.Set<ExpenseCategory>();

    public async Task AddTransactionAsync(
        FinancialTransaction transaction,
        CancellationToken cancellationToken)
    {
        await context.Set<FinancialTransaction>().AddAsync(transaction, cancellationToken);
    }
}
