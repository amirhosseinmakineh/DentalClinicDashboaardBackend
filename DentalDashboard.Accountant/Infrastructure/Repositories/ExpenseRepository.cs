using DentalDashboard.Accountant.Domain.Entities;
using DentalDashboard.Accountant.Domain.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Accountant.Infrastructure.Repositories;

public sealed class ExpenseRepository(DbContext context)
    : AccountantRepositoryBase<long, ExpenseCategory>(context),
      IExpenseRepository;
