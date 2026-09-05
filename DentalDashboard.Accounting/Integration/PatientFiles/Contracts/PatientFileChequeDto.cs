using DentalDashboard.Accounting.Domain.PatientFinance.Enums;

namespace DentalDashboard.ApplicationService.Contract.Secretary.PatientFiles;

public sealed record PatientFileChequeDto(
    long Id,
    decimal Amount,
    string SayadNumber,
    string OwnerName,
    DateTime DueDate,
    PatientChequeStatus Status);
