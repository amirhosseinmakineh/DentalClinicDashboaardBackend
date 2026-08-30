using DentalDashboard.Domain.Secretary.Account.Entities;
using DentalDashboard.Domain.Secretary.Account.IRepositories;
using DentalDashboard.Infrastracture.Context;
using DentalDashboard.Infrastracture.Repository;

namespace DentalDashboard.Infrastracture.Secretary.Account.Repositories;

public sealed class ExpenseRepository : BaseRepository<long, ExpenseCategory>, IExpenseRepository
{
    public ExpenseRepository(DentalContext context) : base(context)
    {
    }
}
