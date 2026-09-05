using DentalDashboard.Accounting.Domain.Entities;
using DentalDashboard.Framwork.IRepositories;

namespace DentalDashboard.Accounting.Domain.IRepositories;

public interface ISecretaryAccountRepository
{
    IQueryable<FinancialTransaction> FinancialTransactions { get; }
    IQueryable<ExpenseCategory> ExpenseCategories { get; }
    Task AddTransactionAsync(FinancialTransaction transaction, CancellationToken cancellationToken);
}
