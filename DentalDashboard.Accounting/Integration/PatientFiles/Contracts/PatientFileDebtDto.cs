using DentalDashboard.Accounting.Domain.PatientFinance.Enums;

namespace DentalDashboard.ApplicationService.Contract.Secretary.PatientFiles;

public sealed record PatientFileDebtDto(
    long Id,
    decimal Amount,
    PatientDebtSourceType SourceType,
    long SourceId,
    DateTime DueDate,
    PatientDebtStatus Status);
