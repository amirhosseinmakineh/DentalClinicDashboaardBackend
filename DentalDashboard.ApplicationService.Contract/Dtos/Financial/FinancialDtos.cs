using DentalDashboard.Domain.Enums;

namespace DentalDashboard.ApplicationService.Contract.Dtos.Financial;

public sealed record CreateFinancialTransactionRequest(
    decimal Amount,
    TransactionType TransactionType,
    Guid CreatedByUserId,
    string? ReferenceType = null,
    long? ReferenceId = null,
    string? Description = null);

public sealed record WalletTransactionRequest(decimal Amount, Guid CreatedByUserId, string? Description = null);

public sealed record FinancialTransactionDto(long Id, decimal Amount, TransactionType TransactionType,
    string? ReferenceType, long? ReferenceId, string? Description, DateTime CreatedAt, Guid CreatedByUserId);

public sealed record WalletTransactionDto(long Id, long FinancialTransactionId, decimal Amount,
    WalletTransactionType Type, DateTime CreatedAt, string? Description);

public sealed record WalletDto(long Id, Guid UserId, decimal Balance, DateTime CreatedAt, bool IsActive,
    IReadOnlyCollection<WalletTransactionDto> Transactions);
