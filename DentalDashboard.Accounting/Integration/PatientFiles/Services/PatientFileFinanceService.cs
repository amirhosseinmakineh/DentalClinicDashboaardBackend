using DentalDashboard.ApplicationService.Contract.Secretary.PatientFiles;
using DentalDashboard.Accounting.Domain.PatientFinance.Enums;
using DentalDashboard.Accounting.Domain.PatientFinance.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Accounting.Integration.PatientFiles.Services;

public sealed class PatientFileFinanceService(
    IPatientFinanceRepository patientFinanceRepository)
    : IPatientFileFinanceService
{
    public async Task<List<PatientFileDto>> AttachFinanceAsync(
        IReadOnlyList<PatientFileDto> patientFiles,
        CancellationToken cancellationToken)
    {
        if (patientFiles.Count == 0)
        {
            return [];
        }

        var phoneNumbers = patientFiles
            .Select(patientFile => patientFile.PhoneNumber)
            .Distinct()
            .ToList();

        var financialPatients = await patientFinanceRepository.Patients
            .AsNoTracking()
            .Where(patient =>
                !patient.IsDeleted &&
                patient.PatientProfile != null &&
                !patient.PatientProfile.IsDeleted &&
                phoneNumbers.Contains(patient.PhoneNumber))
            .Select(patient => new
            {
                patient.Id,
                patient.PhoneNumber
            })
            .ToListAsync(cancellationToken);

        var patientIdByPhoneNumber = financialPatients
            .GroupBy(patient => patient.PhoneNumber)
            .ToDictionary(group => group.Key, group => (Guid?)group.First().Id);

        var financialCases = await patientFinanceRepository.Cases
            .AsNoTracking()
            .Where(financialCase =>
                phoneNumbers.Contains(financialCase.Patient.PhoneNumber))
            .OrderByDescending(financialCase => financialCase.CreatedAt)
            .Select(financialCase => new
            {
                PhoneNumber = financialCase.Patient.PhoneNumber,
                financialCase.PatientId,
                Case = new PatientFileFinancialCaseDto(
                    financialCase.Id,
                    (int)financialCase.Service,
                    financialCase.Service.ToString(),
                    financialCase.TotalAmount,
                    financialCase.Transactions.Sum(
                        transaction => (decimal?)transaction.Amount) ?? 0,
                    financialCase.TotalAmount -
                    (financialCase.Transactions.Sum(
                        transaction => (decimal?)transaction.Amount) ?? 0),
                    financialCase.Debts
                        .Where(debt => debt.Status == PatientDebtStatus.Unpaid)
                        .Sum(debt => (decimal?)debt.Amount) ?? 0,
                    financialCase.AgreementType,
                    financialCase.Status,
                    financialCase.CreatedAt,
                    financialCase.Cheques
                        .OrderBy(cheque => cheque.DueDate)
                        .Select(cheque => new PatientFileChequeDto(
                            cheque.Id,
                            cheque.Amount,
                            cheque.SayadNumber,
                            cheque.OwnerName,
                            cheque.DueDate,
                            cheque.Status))
                        .ToList(),
                    financialCase.PromissoryNotes
                        .OrderBy(promissoryNote => promissoryNote.DueDate)
                        .Select(promissoryNote => new PatientFilePromissoryNoteDto(
                            promissoryNote.Id,
                            promissoryNote.SerialNumber,
                            promissoryNote.Amount,
                            promissoryNote.DueDate,
                            promissoryNote.Status))
                        .ToList(),
                    financialCase.Debts
                        .OrderBy(debt => debt.DueDate)
                        .Select(debt => new PatientFileDebtDto(
                            debt.Id,
                            debt.Amount,
                            debt.SourceType,
                            debt.SourceId,
                            debt.DueDate,
                            debt.Status))
                        .ToList(),
                    financialCase.Transactions
                        .OrderByDescending(transaction => transaction.CreatedAt)
                        .Select(transaction => new PatientFileTransactionDto(
                            transaction.Id,
                            transaction.Amount,
                            transaction.Type,
                            transaction.SourceType,
                            transaction.SourceId,
                            transaction.CreatedAt))
                        .ToList())
            })
            .ToListAsync(cancellationToken);

        var financialCasesByPhoneNumber = financialCases
            .GroupBy(financialCase => financialCase.PhoneNumber)
            .ToDictionary(group => group.Key, group => group.ToList());

        return patientFiles.Select(patientFile =>
        {
            patientIdByPhoneNumber.TryGetValue(
                patientFile.PhoneNumber,
                out var financialPatientId);

            if (!financialCasesByPhoneNumber.TryGetValue(
                    patientFile.PhoneNumber,
                    out var patientFinancialCases))
            {
                return patientFile with { FinancialPatientId = financialPatientId };
            }

            var activeFinancialCases = patientFinancialCases
                .Where(financialCase =>
                    financialCase.Case.Status != PatientFinancialCaseStatus.Cancelled)
                .ToList();

            var totalTreatmentAmount = activeFinancialCases.Sum(
                financialCase => financialCase.Case.TotalAmount);
            var totalPaidAmount = activeFinancialCases.Sum(
                financialCase => financialCase.Case.TotalPaidAmount);

            var finance = new PatientFileFinanceDto(
                patientFinancialCases[0].PatientId,
                totalTreatmentAmount,
                totalPaidAmount,
                totalTreatmentAmount - totalPaidAmount,
                activeFinancialCases.Sum(
                    financialCase => financialCase.Case.TotalDebtAmount),
                patientFinancialCases.Count(financialCase =>
                    financialCase.Case.Status == PatientFinancialCaseStatus.Active),
                activeFinancialCases.Sum(financialCase =>
                    financialCase.Case.Cheques.Count(cheque =>
                        cheque.Status == PatientChequeStatus.Unpaid)),
                activeFinancialCases.Sum(financialCase =>
                    financialCase.Case.PromissoryNotes.Count(promissoryNote =>
                        promissoryNote.Status == PatientPromissoryNoteStatus.Unpaid)),
                patientFinancialCases
                    .Select(financialCase => financialCase.Case)
                    .ToList());

            return patientFile with
            {
                FinancialPatientId = financialPatientId ?? patientFinancialCases[0].PatientId,
                Finance = finance
            };
        }).ToList();
    }
}
