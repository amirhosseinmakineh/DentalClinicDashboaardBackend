using DentalDashboard.Accounting.Contracts.PatientFinance.Queries;
using DentalDashboard.Accounting.Domain.PatientFinance.Enums;
using DentalDashboard.Accounting.Domain.PatientFinance.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Accounting.Application.PatientFinance.Handlers;

public sealed class GetPatientFinancialCaseSummaryQueryHandler(
    IPatientFinanceRepository patientFinanceRepository)
    : IQueryHandler<
        GetPatientFinancialCaseSummaryQuery,
        PatientFinancialCaseSummaryDto?>
{
    public Task<PatientFinancialCaseSummaryDto?> HandleAsync(
        GetPatientFinancialCaseSummaryQuery request,
        CancellationToken cancellationToken = default)
    {
        return patientFinanceRepository.Cases
            .AsNoTracking()
            .Where(financialCase =>
                financialCase.Id == request.PatientFinancialCaseId)
            .Select(financialCase => new PatientFinancialCaseSummaryDto(
                financialCase.TotalAmount,
                financialCase.Transactions
                    .Where(transaction =>
                        transaction.Type == PatientFinancialTransactionType.Payment)
                    .Sum(transaction => (decimal?)transaction.Amount) ?? 0,
                Math.Max(
                    financialCase.TotalAmount -
                    (financialCase.Transactions
                        .Where(transaction =>
                            transaction.Type == PatientFinancialTransactionType.Payment)
                        .Sum(transaction => (decimal?)transaction.Amount) ?? 0),
                    0),
                financialCase.Cheques
                    .Where(cheque => cheque.Status != PatientChequeStatus.Cancelled)
                    .Sum(cheque => (decimal?)cheque.Amount) ?? 0,
                financialCase.Cheques
                    .Where(cheque => cheque.Status == PatientChequeStatus.Paid)
                    .Sum(cheque => (decimal?)cheque.Amount) ?? 0,
                financialCase.Cheques
                    .Where(cheque => cheque.Status == PatientChequeStatus.Pending)
                    .Sum(cheque => (decimal?)cheque.Amount) ?? 0,
                financialCase.Cheques
                    .Where(cheque => cheque.Status == PatientChequeStatus.Unpaid)
                    .Sum(cheque => (decimal?)cheque.Amount) ?? 0,
                financialCase.PromissoryNotes
                    .Where(promissoryNote =>
                        promissoryNote.Status != PatientPromissoryNoteStatus.Cancelled)
                    .Sum(promissoryNote => (decimal?)promissoryNote.Amount) ?? 0,
                financialCase.PromissoryNotes
                    .Where(promissoryNote =>
                        promissoryNote.Status == PatientPromissoryNoteStatus.Paid)
                    .Sum(promissoryNote => (decimal?)promissoryNote.Amount) ?? 0,
                financialCase.PromissoryNotes
                    .Where(promissoryNote =>
                        promissoryNote.Status == PatientPromissoryNoteStatus.Pending)
                    .Sum(promissoryNote => (decimal?)promissoryNote.Amount) ?? 0,
                financialCase.PromissoryNotes
                    .Where(promissoryNote =>
                        promissoryNote.Status == PatientPromissoryNoteStatus.Unpaid)
                    .Sum(promissoryNote => (decimal?)promissoryNote.Amount) ?? 0,
                financialCase.Debts
                    .Where(debt => debt.Status == PatientDebtStatus.Unpaid)
                    .Sum(debt => (decimal?)debt.Amount) ?? 0))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
