using DentalDashboard.Accountant.Domain.Entities;
using DentalDashboard.Framwork.IRepositories;

namespace DentalDashboard.Accountant.Domain.IRepositories;

public interface IAccountantRepository
{
    IQueryable<FinancialTransaction> FinancialTransactions { get; }
    IQueryable<ExpenseCategory> ExpenseCategories { get; }
    Task AddTransactionAsync(FinancialTransaction transaction, CancellationToken cancellationToken);
}

public interface IExpenseRepository : IBaseRepository<long, ExpenseCategory>
{

}
