using DentalDashboard.Accounting.Domain.Entities;
using DentalDashboard.Framwork.IRepositories;

namespace DentalDashboard.Accounting.Domain.IRepositories;

public interface IExpenseRepository : IBaseRepository<long, ExpenseCategory>
{

}
