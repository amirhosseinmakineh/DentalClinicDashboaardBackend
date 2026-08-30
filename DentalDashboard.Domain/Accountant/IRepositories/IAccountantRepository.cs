using DentalDashboard.Domain.Accountant.Entities;
using DentalDashboard.Framwork.IRepositories;

namespace DentalDashboard.Domain.Accountant.IRepositories;

public interface IAccountantRepository
{
    IQueryable<FinancialTransaction> FinancialTransactions { get; }
    IQueryable<ExpenseCategory> ExpenseCategories { get; }
    Task AddTransactionAsync(FinancialTransaction transaction, CancellationToken cancellationToken);
}

public interface IExpenseRepository : IBaseRepository<long, ExpenseCategory>
{

}
