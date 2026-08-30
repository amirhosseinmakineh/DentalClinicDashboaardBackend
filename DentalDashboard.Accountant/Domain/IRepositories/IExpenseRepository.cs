using DentalDashboard.Accountant.Domain.Entities;
using DentalDashboard.Framwork.IRepositories;

namespace DentalDashboard.Accountant.Domain.IRepositories;

public interface IExpenseRepository : IBaseRepository<long, ExpenseCategory>
{
}
