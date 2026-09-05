using DentalDashboard.Accounting.Domain.SecretarySales.Enums;

namespace DentalDashboard.Accounting.Contracts.SecretarySales;

public sealed record SecretaryWalletTransactionDto(
    long Id,
    decimal Amount,
    SecretaryWalletTransactionType TransactionType,
    string Description,
    DateTime CreatedAt,
    long? SaleId,
    string? ServiceTitle,
    string? PatientName);
