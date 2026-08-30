using DentalDashboard.Accountant.Domain.PatientFinance.Entities;
using DentalDashboard.Accountant.Domain.PatientFinance.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Accountant.Infrastructure.PatientFinance.Repositories;
public sealed class PatientFinanceRepository(DbContext context)
    : IPatientFinanceRepository {
  public IQueryable<PatientFinancialCase> Cases =>
      context.Set<PatientFinancialCase>();
  public IQueryable<PatientCheque> Cheques => context.Set<PatientCheque>();
  public IQueryable<PatientPromissoryNote> PromissoryNotes =>
      context.Set<PatientPromissoryNote>();
  public IQueryable<PatientDebt> Debts => context.Set<PatientDebt>();
  public IQueryable<PatientFinancialTransaction> Transactions =>
      context.Set<PatientFinancialTransaction>();
  public IQueryable<DentalDashboard.Domain.Models.User> Patients =>
      context.Set<DentalDashboard.Domain.Models.User>();
  public Task AddCaseAsync(PatientFinancialCase x, CancellationToken ct) =>
      context.Set<PatientFinancialCase>().AddAsync(x, ct).AsTask();
  public Task AddChequeAsync(PatientCheque x, CancellationToken ct) =>
      context.Set<PatientCheque>().AddAsync(x, ct).AsTask();
  public Task AddPromissoryNoteAsync(PatientPromissoryNote x,
                                     CancellationToken ct) =>
      context.Set<PatientPromissoryNote>().AddAsync(x, ct).AsTask();
  public Task AddDebtAsync(PatientDebt x, CancellationToken ct) =>
      context.Set<PatientDebt>().AddAsync(x, ct).AsTask();
  public Task AddTransactionAsync(PatientFinancialTransaction x,
                                  CancellationToken ct) =>
      context.Set<PatientFinancialTransaction>().AddAsync(x, ct).AsTask();
}
