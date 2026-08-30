using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Entities;
using DentalDashboard.Domain.Models;

namespace DentalDashboard.Domain.Secretary.Accountant.PatientFinance.IRepositories;

public interface IPatientFinanceRepository {
  IQueryable<PatientFinancialCase> Cases { get; }
  IQueryable<PatientCheque> Cheques { get; }
  IQueryable<PatientPromissoryNote> PromissoryNotes { get; }
  IQueryable<PatientDebt> Debts { get; }
  IQueryable<PatientFinancialTransaction> Transactions { get; }
  IQueryable<User> Patients { get; }
  Task AddCaseAsync(PatientFinancialCase entity, CancellationToken ct);
  Task AddChequeAsync(PatientCheque entity, CancellationToken ct);
  Task AddPromissoryNoteAsync(PatientPromissoryNote entity,
                              CancellationToken ct);
  Task AddDebtAsync(PatientDebt entity, CancellationToken ct);
  Task AddTransactionAsync(PatientFinancialTransaction entity,
                           CancellationToken ct);
}
