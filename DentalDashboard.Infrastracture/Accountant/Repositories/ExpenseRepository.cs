using DentalDashboard.Domain.Accountant.Entities;
using DentalDashboard.Domain.Accountant.IRepositories;
using DentalDashboard.Infrastracture.Context;
using DentalDashboard.Infrastracture.Repository;

namespace DentalDashboard.Infrastracture.Accountant.Repositories;

public sealed class ExpenseRepository : BaseRepository<long, ExpenseCategory>, IExpenseRepository
{
    public ExpenseRepository(DentalContext context) : base(context)
    {
    }
}
