using DentalDashboard.Accounting.Domain.Entities;
using DentalDashboard.Accounting.Domain.IRepositories;
using DentalDashboard.Infrastracture.Context;
using DentalDashboard.Infrastracture.Repository;

namespace DentalDashboard.Accounting.Infrastructure.Repositories;

public sealed class ExpenseRepository : BaseRepository<long, ExpenseCategory>, IExpenseRepository
{
    public ExpenseRepository(DentalContext context) : base(context)
    {
    }
}
