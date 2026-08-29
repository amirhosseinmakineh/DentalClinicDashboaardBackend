using DentalDashboard.Domain.Secretary.Account.Entities;

namespace DentalDashboard.Domain.Secretary.Account.Repositories;

public interface ISecretaryAccountRepository
{
    IQueryable<FinancialTransaction> FinancialTransactions { get; }
    IQueryable<ExpenseCategory> ExpenseCategories { get; }
    Task AddTransactionAsync(FinancialTransaction transaction, CancellationToken cancellationToken);
}
