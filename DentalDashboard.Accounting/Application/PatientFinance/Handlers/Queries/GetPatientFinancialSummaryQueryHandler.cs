using System.Globalization;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Accounting.Contracts.PatientFinance.Queries;
using DentalDashboard.Domain.Models;
using DentalDashboard.Accounting.Domain.PatientFinance.Entities;
using DentalDashboard.Accounting.Domain.PatientFinance.Enums;
using DentalDashboard.Accounting.Domain.PatientFinance.IRepositories;
using DentalDashboard.Utilities.Time;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Accounting.Application.PatientFinance.Handlers;

public sealed class GetPatientFinancialSummaryQueryHandler(
    IPatientFinanceRepository patientFinanceRepository)
    : IQueryHandler<GetPatientFinancialSummaryQuery, PatientFinancialSummaryDto?>
{
    public async Task<PatientFinancialSummaryDto?> HandleAsync(
        GetPatientFinancialSummaryQuery request,
        CancellationToken cancellationToken = default)
    {
        if (!await patientFinanceRepository.Cases.AnyAsync(
                financialCase => financialCase.PatientId == request.PatientId,
                cancellationToken))
            return null;

        var totalAmount = await patientFinanceRepository.Cases
            .Where(financialCase =>
                financialCase.PatientId == request.PatientId &&
                financialCase.Status != PatientFinancialCaseStatus.Cancelled)
            .SumAsync(
                financialCase => (decimal?)financialCase.TotalAmount,
                cancellationToken) ?? 0;

        var paidAmount = await patientFinanceRepository.Transactions
            .Where(transaction =>
                transaction.FinancialCase.PatientId == request.PatientId)
            .SumAsync(
                transaction => (decimal?)transaction.Amount,
                cancellationToken) ?? 0;

        return new(
            request.PatientId,
            totalAmount,
            paidAmount,
            totalAmount - paidAmount,
            await patientFinanceRepository.Debts
                .Where(debt =>
                    debt.FinancialCase.PatientId == request.PatientId &&
                    debt.Status == PatientDebtStatus.Unpaid)
                .SumAsync(debt => (decimal?)debt.Amount, cancellationToken) ?? 0,
            await patientFinanceRepository.Cases.CountAsync(
                financialCase =>
                    financialCase.PatientId == request.PatientId &&
                    financialCase.Status == PatientFinancialCaseStatus.Active,
                cancellationToken),
            await patientFinanceRepository.Cheques.CountAsync(
                cheque =>
                    cheque.FinancialCase.PatientId == request.PatientId &&
                    cheque.Status == PatientChequeStatus.Unpaid,
                cancellationToken),
            await patientFinanceRepository.PromissoryNotes.CountAsync(
                promissoryNote =>
                    promissoryNote.FinancialCase.PatientId == request.PatientId &&
                    promissoryNote.Status == PatientPromissoryNoteStatus.Unpaid,
                cancellationToken));
    }
}
