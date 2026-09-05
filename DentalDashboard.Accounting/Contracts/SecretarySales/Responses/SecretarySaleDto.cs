using DentalDashboard.Accounting.Domain.SecretarySales.Enums;

namespace DentalDashboard.Accounting.Contracts.SecretarySales;

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
