using DentalDashboard.Accounting.Domain.PatientFinance.Entities;
using DentalDashboard.Accounting.Domain.PatientFinance.IRepositories;
using DentalDashboard.Infrastracture.Context;

namespace DentalDashboard.Accounting.Infrastructure.PatientFinance.Repositories;

public sealed class PatientFinanceRepository(DentalContext context)
    : IPatientFinanceRepository
{
    public IQueryable<PatientFinancialCase> Cases => context.PatientFinancialCases;

    public IQueryable<PatientCheque> Cheques => context.PatientCheques;

    public IQueryable<PatientPromissoryNote> PromissoryNotes =>
        context.PatientPromissoryNotes;

    public IQueryable<PatientDebt> Debts => context.PatientDebts;

    public IQueryable<PatientFinancialTransaction> Transactions =>
        context.PatientFinancialTransactions;

    public IQueryable<DentalDashboard.Domain.Models.User> Patients => context.Users;

    public IQueryable<DentalDashboard.Domain.Models.PatientFile> PatientFiles =>
        context.PatientFiles;

    public Task AddCaseAsync(
        PatientFinancialCase financialCase,
        CancellationToken cancellationToken)
    {
        return context.PatientFinancialCases
            .AddAsync(financialCase, cancellationToken)
            .AsTask();
    }

    public Task AddChequeAsync(
        PatientCheque cheque,
        CancellationToken cancellationToken)
    {
        return context.PatientCheques.AddAsync(cheque, cancellationToken).AsTask();
    }

    public Task AddPromissoryNoteAsync(
        PatientPromissoryNote promissoryNote,
        CancellationToken cancellationToken)
    {
        return context.PatientPromissoryNotes
            .AddAsync(promissoryNote, cancellationToken)
            .AsTask();
    }

    public Task AddDebtAsync(
        PatientDebt debt,
        CancellationToken cancellationToken)
    {
        return context.PatientDebts.AddAsync(debt, cancellationToken).AsTask();
    }

    public Task AddTransactionAsync(
        PatientFinancialTransaction transaction,
        CancellationToken cancellationToken)
    {
        return context.PatientFinancialTransactions
            .AddAsync(transaction, cancellationToken)
            .AsTask();
    }
}
