using DentalDashboard.Accounting.Domain.SecretarySales.Enums;

namespace DentalDashboard.Accounting.Contracts.SecretarySales;

public sealed record SecretarySaleServiceDto(
    long Id,
    string Title,
    decimal Price,
    decimal SecretaryReward,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
