using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Enums;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Domain.Models;

namespace DentalDashboard.ApplicationService.Contract.Secretary.Accountant.PatientFinance
    .Queries;

public sealed record PatientFinancialCaseDto(
    Guid Id, Guid PatientId, Guid UserId, string PatientName,
    string PatientFileNumber, string? PatientPhoneNumber, int ServiceId, string ServiceName, decimal TotalAmount,
    decimal PrePaymentAmount, decimal DepositAmount,
    decimal TotalPaidAmount, decimal RemainingAmount, decimal TotalDebtAmount,
    PatientFinancialAgreementType AgreementType,
    PatientFinancialCaseStatus Status, DateTime CreatedAt);
