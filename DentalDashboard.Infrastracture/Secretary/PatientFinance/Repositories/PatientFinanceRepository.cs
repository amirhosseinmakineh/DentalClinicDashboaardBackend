using DentalDashboard.Domain.Secretary.PatientFinance.Entities;
using DentalDashboard.Domain.Secretary.PatientFinance.IRepositories;
using DentalDashboard.Infrastracture.Context;

namespace DentalDashboard.Infrastracture.Secretary.PatientFinance.Repositories;
public sealed class PatientFinanceRepository(DentalContext context)
    : IPatientFinanceRepository {
  public IQueryable<PatientFinancialCase> Cases =>
      context.PatientFinancialCases;
  public IQueryable<PatientCheque> Cheques => context.PatientCheques;
  public IQueryable<PatientPromissoryNote> PromissoryNotes =>
      context.PatientPromissoryNotes;
  public IQueryable<PatientDebt> Debts => context.PatientDebts;
  public IQueryable<PatientFinancialTransaction> Transactions =>
      context.PatientFinancialTransactions;
  public IQueryable<DentalDashboard.Domain.Models.PatientProfile> Patients =>
      context.PatientProfiles;
  public Task AddCaseAsync(PatientFinancialCase x, CancellationToken ct) =>
      context.PatientFinancialCases.AddAsync(x, ct).AsTask();
  public Task AddChequeAsync(PatientCheque x, CancellationToken ct) =>
      context.PatientCheques.AddAsync(x, ct).AsTask();
  public Task AddPromissoryNoteAsync(PatientPromissoryNote x,
                                     CancellationToken ct) =>
      context.PatientPromissoryNotes.AddAsync(x, ct).AsTask();
  public Task AddDebtAsync(PatientDebt x, CancellationToken ct) =>
      context.PatientDebts.AddAsync(x, ct).AsTask();
  public Task AddTransactionAsync(PatientFinancialTransaction x,
                                  CancellationToken ct) =>
      context.PatientFinancialTransactions.AddAsync(x, ct).AsTask();
}
