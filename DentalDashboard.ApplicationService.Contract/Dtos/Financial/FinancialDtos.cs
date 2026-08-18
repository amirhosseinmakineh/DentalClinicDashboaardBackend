using DentalDashboard.Domain.Enums;

namespace DentalDashboard.ApplicationService.Contract.Dtos.Financial;

public sealed record CreateFinancialTransactionRequest(
    decimal Amount,
    TransactionType TransactionType,
    TransactionDirection Direction,
    FinancialTransactionStatus Status = FinancialTransactionStatus.Completed,
    string? ReferenceType = null,
    long? ReferenceId = null,
    string? Description = null);

public sealed record WalletTransactionRequest(decimal Amount, string? Description = null);

public sealed record FinancialTransactionDto(long Id, decimal Amount, TransactionType TransactionType,
    TransactionDirection Direction, FinancialTransactionStatus Status, string? ReferenceType, long? ReferenceId,
    string? Description, DateTime CreatedAt, Guid CreatedByUserId, DateTime? UpdatedAt, Guid? UpdatedByUserId);

public sealed record WalletTransactionDto(long Id, long FinancialTransactionId, decimal Amount,
    WalletTransactionType Type, DateTime CreatedAt, string? Description);

public sealed record WalletDto(long Id, Guid UserId, decimal Balance, DateTime CreatedAt, bool IsActive,
    IReadOnlyCollection<WalletTransactionDto> Transactions, int TotalCount, int Page, int PageSize);
