using DentalDashboard.Accounting.Domain.PatientFinance.Enums;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Domain.Models;

namespace DentalDashboard.Accounting.Contracts.PatientFinance
    .Queries;

public sealed record PatientFinancialSummaryDto(
    Guid PatientId, decimal TotalTreatmentAmount, decimal TotalPaidAmount,
    decimal RemainingAmount, decimal TotalDebtAmount,
    int ActiveFinancialCasesCount, int UnpaidChequesCount,
    int UnpaidPromissoryNotesCount);
