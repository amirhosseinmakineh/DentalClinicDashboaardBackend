using DentalDashboard.Accounting.Domain.PatientFinance.Enums;

namespace DentalDashboard.ApplicationService.Contract.Secretary.PatientFiles;

public sealed record PatientFileTransactionDto(
    long Id,
    decimal Amount,
    PatientFinancialTransactionType Type,
    PatientFinancialTransactionSourceType SourceType,
    long SourceId,
    DateTime CreatedAt);
