using DentalDashboard.Domain.Accountant.Entities;
using DentalDashboard.Framwork.IRepositories;

namespace DentalDashboard.Domain.Accountant.IRepositories;

public interface IExpenseRepository : IBaseRepository<long, ExpenseCategory>
{
}
