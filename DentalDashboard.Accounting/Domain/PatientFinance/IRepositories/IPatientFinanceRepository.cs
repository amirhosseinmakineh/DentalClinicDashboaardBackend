using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Entities;
using DentalDashboard.Domain.Models;

namespace DentalDashboard.Domain.Secretary.Accountant.PatientFinance.IRepositories;

public interface IPatientFinanceRepository
{
    IQueryable<PatientFinancialCase> Cases { get; }
    IQueryable<PatientCheque> Cheques { get; }
    IQueryable<PatientPromissoryNote> PromissoryNotes { get; }
    IQueryable<PatientDebt> Debts { get; }
    IQueryable<PatientFinancialTransaction> Transactions { get; }
    IQueryable<User> Patients { get; }
    IQueryable<PatientFile> PatientFiles { get; }
    Task AddCaseAsync(PatientFinancialCase entity, CancellationToken cancellationToken);
    Task AddChequeAsync(PatientCheque entity, CancellationToken cancellationToken);
    Task AddPromissoryNoteAsync(
        PatientPromissoryNote entity,
        CancellationToken cancellationToken);
    Task AddDebtAsync(PatientDebt entity, CancellationToken cancellationToken);
    Task AddTransactionAsync(
        PatientFinancialTransaction entity,
        CancellationToken cancellationToken);
}
