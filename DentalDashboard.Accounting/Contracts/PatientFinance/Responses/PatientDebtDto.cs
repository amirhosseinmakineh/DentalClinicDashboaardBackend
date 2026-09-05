using DentalDashboard.Accounting.Domain.PatientFinance.Enums;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Domain.Models;

namespace DentalDashboard.Accounting.Contracts.PatientFinance
    .Queries;

public sealed record PatientDebtDto(long Id, Guid PatientId, string PatientName,
                                    string PatientFileNumber,
                                    string? PatientPhoneNumber,
                                    Guid PatientFinancialCaseId,
                                    string ServiceName, decimal Amount,
                                    PatientDebtSourceType SourceType,
                                    long SourceId, DateTime DueDate,
                                    PatientDebtStatus Status);
