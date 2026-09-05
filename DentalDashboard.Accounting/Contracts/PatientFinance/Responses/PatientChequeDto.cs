using DentalDashboard.Accounting.Domain.PatientFinance.Enums;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Domain.Models;

namespace DentalDashboard.Accounting.Contracts.PatientFinance
    .Queries;

public sealed record PatientChequeDto(long Id, Guid PatientFinancialCaseId,
                                      Guid PatientId, string PatientName,
                                      string PatientFileNumber,
                                      decimal Amount, string SayadNumber,
                                      string OwnerName, DateTime DueDate,
                                      PatientChequeStatus Status);
