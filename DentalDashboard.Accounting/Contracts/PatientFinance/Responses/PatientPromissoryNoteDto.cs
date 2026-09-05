using DentalDashboard.Accounting.Domain.PatientFinance.Enums;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Domain.Models;

namespace DentalDashboard.Accounting.Contracts.PatientFinance
    .Queries;

public sealed record PatientPromissoryNoteDto(
    long Id, Guid PatientFinancialCaseId, Guid PatientId, string PatientName,
    string PatientFileNumber,
    string SerialNumber, decimal Amount, DateTime DueDate,
    PatientPromissoryNoteStatus Status);
