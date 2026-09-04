using DentalDashboard.Domain.Secretary.Accountant.SecretarySales.Enums;

namespace DentalDashboard.ApplicationService.Contract.Secretary.Accountant.SecretarySales;

public sealed record SecretarySaleDto(
    long SaleId,
    Guid SecretaryUserId,
    string SecretaryName,
    Guid PatientUserId,
    string PatientName,
    string PatientPhoneNumber,
    long ServiceId,
    string ServiceTitle,
    decimal SalePrice,
    decimal SecretaryReward,
    SecretarySaleStatus Status,
    DateTime CreatedAt,
    DateTime? ReviewedAt);
