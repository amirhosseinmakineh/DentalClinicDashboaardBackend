using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Enums;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Domain.Models;

namespace DentalDashboard.ApplicationService.Contract.Secretary.Accountant.PatientFinance
    .Queries;

public sealed record PatientFinancialCommitmentDto(
    long Id, PatientFinancialCommitmentType Type, Guid PatientFinancialCaseId,
    Guid PatientId, string PatientName, string PatientFileNumber,
    decimal Amount, DateTime DueDate,
    int Status);
