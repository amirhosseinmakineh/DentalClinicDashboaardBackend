using DentalDashboard.Domain.Secretary.Accountant.SecretarySales.Enums;

namespace DentalDashboard.ApplicationService.Contract.Secretary.Accountant.SecretarySales;

public sealed record SecretaryWalletTransactionDto(
    long Id,
    decimal Amount,
    SecretaryWalletTransactionType TransactionType,
    string Description,
    DateTime CreatedAt,
    long? SaleId,
    string? ServiceTitle,
    string? PatientName);
