using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Accounting.Contracts.PatientFinance.Queries;
using DentalDashboard.Accounting.Domain.PatientFinance.Enums;
using DentalDashboard.Accounting.Domain.PatientFinance.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Accounting.Application.PatientFinance.Handlers;

public sealed class GetPatientFinancialCaseDetailsQueryHandler(
    IPatientFinanceRepository patientFinanceRepository)
    : IQueryHandler<GetPatientFinancialCaseDetailsQuery, PatientFinancialCaseDetailsDto?>
{
    public Task<PatientFinancialCaseDetailsDto?> HandleAsync(
        GetPatientFinancialCaseDetailsQuery request,
        CancellationToken cancellationToken = default)
    {
        return patientFinanceRepository.Cases
            .AsNoTracking()
            .Where(financialCase =>
                financialCase.Id == request.PatientFinancialCaseId)
            .Select(financialCase => new PatientFinancialCaseDetailsDto(
                new(
                    financialCase.Id,
                    financialCase.PatientId,
                    financialCase.Patient.Id,
                    (financialCase.Patient.FirstName + " " +
                     financialCase.Patient.LastName).Trim(),
                    patientFinanceRepository.PatientFiles
                        .Where(patientFile =>
                            patientFile.PhoneNumber == financialCase.Patient.PhoneNumber)
                        .Select(patientFile => patientFile.FileNumber.ToString())
                        .FirstOrDefault() ?? "",
                    financialCase.Patient.PhoneNumber,
                    (int)financialCase.Service,
                    financialCase.Service == DentalDashboard.Domain.Enums.DentalServiceType.Composite
                        ? "کامپوزیت"
                        : financialCase.Service == DentalDashboard.Domain.Enums.DentalServiceType.Implant
                            ? "ایمپلنت"
                            : financialCase.Service == DentalDashboard.Domain.Enums.DentalServiceType.Laminate
                                ? "لمینت"
                                : financialCase.Service.ToString(),
                    financialCase.TotalAmount,
                    financialCase.PrePaymentAmount,
                    financialCase.DepositAmount,
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
                    financialCase.Debts
                        .Where(debt => debt.Status == PatientDebtStatus.Unpaid)
                        .Sum(debt => (decimal?)debt.Amount) ?? 0,
                    financialCase.AgreementType,
                    financialCase.Status,
                    financialCase.CreatedAt),
                financialCase.Cheques.Count(
                    cheque => cheque.Status != PatientChequeStatus.Cancelled),
                financialCase.Cheques
                    .Where(cheque => cheque.Status != PatientChequeStatus.Cancelled)
                    .Sum(cheque => (decimal?)cheque.Amount) ?? 0,
                financialCase.PromissoryNotes.Count(
                    promissoryNote =>
                        promissoryNote.Status != PatientPromissoryNoteStatus.Cancelled),
                financialCase.PromissoryNotes
                    .Where(promissoryNote =>
                        promissoryNote.Status != PatientPromissoryNoteStatus.Cancelled)
                    .Sum(promissoryNote => (decimal?)promissoryNote.Amount) ?? 0,
                financialCase.Cheques
                    .OrderBy(cheque => cheque.DueDate)
                    .ThenBy(cheque => cheque.Id)
                    .Select(cheque => new PatientChequeDto(
                        cheque.Id,
                        cheque.PatientFinancialCaseId,
                        financialCase.PatientId,
                        (financialCase.Patient.FirstName + " " +
                         financialCase.Patient.LastName).Trim(),
                        patientFinanceRepository.PatientFiles
                            .Where(patientFile =>
                                patientFile.PhoneNumber == financialCase.Patient.PhoneNumber)
                            .Select(patientFile => patientFile.FileNumber.ToString())
                            .FirstOrDefault() ?? "",
                        cheque.Amount,
                        cheque.SayadNumber,
                        cheque.OwnerName,
                        cheque.DueDate,
                        cheque.Status))
                    .ToList(),
                financialCase.PromissoryNotes
                    .OrderBy(promissoryNote => promissoryNote.DueDate)
                    .ThenBy(promissoryNote => promissoryNote.Id)
                    .Select(promissoryNote => new PatientPromissoryNoteDto(
                        promissoryNote.Id,
                        promissoryNote.PatientFinancialCaseId,
                        financialCase.PatientId,
                        (financialCase.Patient.FirstName + " " +
                         financialCase.Patient.LastName).Trim(),
                        patientFinanceRepository.PatientFiles
                            .Where(patientFile =>
                                patientFile.PhoneNumber == financialCase.Patient.PhoneNumber)
                            .Select(patientFile => patientFile.FileNumber.ToString())
                            .FirstOrDefault() ?? "",
                        promissoryNote.SerialNumber,
                        promissoryNote.Amount,
                        promissoryNote.DueDate,
                        promissoryNote.Status))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
