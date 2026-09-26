using DentalDashboard.Domain.Secretary.Accountant.Entities;
using DentalDashboard.Framwork.IRepositories;

namespace DentalDashboard.Domain.Secretary.Accountant.IRepositories;

public interface ISecretaryAccountRepository
{
    IQueryable<FinancialTransaction> FinancialTransactions { get; }
    IQueryable<ExpenseCategory> ExpenseCategories { get; }
    Task AddTransactionAsync(FinancialTransaction transaction, CancellationToken cancellationToken);
}
