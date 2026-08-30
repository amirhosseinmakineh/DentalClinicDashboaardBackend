using DentalDashboard.Domain.Secretary.Account.Entities;
using DentalDashboard.Framwork.IRepositories;

namespace DentalDashboard.Domain.Secretary.Account.IRepositories;

public interface ISecretaryAccountRepository
{
    IQueryable<FinancialTransaction> FinancialTransactions { get; }
    IQueryable<ExpenseCategory> ExpenseCategories { get; }
    Task AddTransactionAsync(FinancialTransaction transaction, CancellationToken cancellationToken);
}

public interface IExpenseRepository : IBaseRepository<long, ExpenseCategory>
{

}
