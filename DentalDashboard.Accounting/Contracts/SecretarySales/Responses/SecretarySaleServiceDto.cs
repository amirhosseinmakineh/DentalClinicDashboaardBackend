using DentalDashboard.Domain.Secretary.Accountant.SecretarySales.Enums;

namespace DentalDashboard.ApplicationService.Contract.Secretary.Accountant.SecretarySales;

public sealed record SecretarySaleServiceDto(
    long Id,
    string Title,
    decimal Price,
    decimal SecretaryReward,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
