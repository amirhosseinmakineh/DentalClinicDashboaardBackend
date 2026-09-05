using DentalDashboard.Accounting.Domain.PatientFinance.Enums;

namespace DentalDashboard.ApplicationService.Contract.Secretary.PatientFiles;

public sealed record PatientFileFinancialCaseDto(
    Guid Id,
    int ServiceId,
    string ServiceName,
    decimal TotalAmount,
    decimal TotalPaidAmount,
    decimal RemainingAmount,
    decimal TotalDebtAmount,
    PatientFinancialAgreementType AgreementType,
    PatientFinancialCaseStatus Status,
    DateTime CreatedAt,
    IReadOnlyList<PatientFileChequeDto> Cheques,
    IReadOnlyList<PatientFilePromissoryNoteDto> PromissoryNotes,
    IReadOnlyList<PatientFileDebtDto> Debts,
    IReadOnlyList<PatientFileTransactionDto> Transactions);
