namespace DentalDashboard.ApplicationService.Contract.Secretary.PatientFiles;

public sealed record PatientFileFinanceDto(
    Guid FinancialPatientId,
    decimal TotalTreatmentAmount,
    decimal TotalPaidAmount,
    decimal RemainingAmount,
    decimal TotalDebtAmount,
    int ActiveFinancialCasesCount,
    int UnpaidChequesCount,
    int UnpaidPromissoryNotesCount,
    IReadOnlyList<PatientFileFinancialCaseDto> Cases);
