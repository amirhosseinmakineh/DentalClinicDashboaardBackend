using DentalDashboard.Domain.Secretary.Accountant.Entities;
using DentalDashboard.Framwork.IRepositories;

namespace DentalDashboard.Domain.Secretary.Accountant.IRepositories;

public interface IExpenseRepository : IBaseRepository<long, ExpenseCategory>
{

}
