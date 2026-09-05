using DentalDashboard.Accounting.Domain.PatientFinance.Enums;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Domain.Models;

namespace DentalDashboard.Accounting.Contracts.PatientFinance
    .Queries;

public sealed record PatientFinancialCaseSummaryDto(
    decimal TotalAmount, decimal TotalPaidAmount, decimal RemainingAmount,
    decimal TotalChequeAmount, decimal PaidChequeAmount,
    decimal PendingChequeAmount, decimal UnpaidChequeAmount,
    decimal TotalPromissoryNoteAmount, decimal PaidPromissoryNoteAmount,
    decimal PendingPromissoryNoteAmount, decimal UnpaidPromissoryNoteAmount,
    decimal TotalDebtAmount);
