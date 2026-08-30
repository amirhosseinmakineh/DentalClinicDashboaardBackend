using DentalDashboard.Domain.Secretary.Account.Entities;
using DentalDashboard.Framwork.IRepositories;

namespace DentalDashboard.Domain.Secretary.Account.IRepositories;

public interface IExpenseRepository : IBaseRepository<long, ExpenseCategory>
{
}
