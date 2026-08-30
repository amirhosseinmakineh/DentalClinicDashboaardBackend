using DentalDashboard.Domain.Secretary.Accountant.Entities;
using DentalDashboard.Domain.Secretary.Accountant.IRepositories;
using DentalDashboard.Infrastracture.Context;
using DentalDashboard.Infrastracture.Repository;

namespace DentalDashboard.Infrastracture.Secretary.Accountant.Repositories;

public sealed class ExpenseRepository : BaseRepository<long, ExpenseCategory>, IExpenseRepository
{
    public ExpenseRepository(DentalContext context) : base(context)
    {
    }
}
