using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Enums;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Domain.Models;

namespace DentalDashboard.ApplicationService.Contract.Secretary.Accountant.PatientFinance
    .Queries;

public sealed record PatientFinancialSummaryDto(
    Guid PatientId, decimal TotalTreatmentAmount, decimal TotalPaidAmount,
    decimal RemainingAmount, decimal TotalDebtAmount,
    int ActiveFinancialCasesCount, int UnpaidChequesCount,
    int UnpaidPromissoryNotesCount);
