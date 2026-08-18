using DentalDashboard.ApplicationService.Contract.Dtos.Financial;
using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.Domain.Constants;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Models;

namespace DentalDashboard.ApplicationService.Services;

public class FinancialTransactionService : IFinancialTransactionService
{
    private const string AdminRole = "Admin";
    private const int MaximumPageSize = 100;
    private readonly IFinancialTransactionRepository repository;
    public FinancialTransactionService(IFinancialTransactionRepository repository) => this.repository = repository;

    public async Task<FinancialTransactionDto> CreateTransactionAsync(CreateFinancialTransactionRequest request,
        Guid createdByUserId, CancellationToken cancellationToken = default)
    {
        ValidateAmount(request.Amount);
        if (!Enum.IsDefined(request.TransactionType)) throw new ArgumentException("Invalid transaction type.");
        if (!Enum.IsDefined(request.Direction)) throw new ArgumentException("Invalid transaction direction.");
        if (!Enum.IsDefined(request.Status)) throw new ArgumentException("Invalid transaction status.");
        await EnsureActiveUserAsync(createdByUserId, "Creating user", cancellationToken);
        var entity = await repository.AddAsync(new FinancialTransaction
        {
            Amount = request.Amount,
            TransactionType = request.TransactionType,
            Direction = request.Direction,
            Status = request.Status,
            ReferenceType = Normalize(request.ReferenceType),
            ReferenceId = request.ReferenceId,
            Description = Normalize(request.Description),
            CreatedByUserId = createdByUserId
        }, cancellationToken);
        return Map(entity);
    }

    public async Task<FinancialTransactionDto> GetTransactionAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0) throw new ArgumentException("Transaction id must be positive.");
        var entity = await repository.GetByIdAsync(id, cancellationToken);
        return entity is null ? throw new KeyNotFoundException("Financial transaction was not found.") : Map(entity);
    }

    public async Task<WalletDto> GetUserWalletAsync(Guid userId, int page = 1, int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        ValidatePagination(page, pageSize);
        await EnsureActiveUserAsync(userId, "User", cancellationToken);
        var result = await repository.GetOrCreateWalletByUserIdAsync(userId, page, pageSize, cancellationToken);
        return Map(result.Wallet, result.TotalCount, page, pageSize);
    }

    public async Task<WalletDto> AddWalletTransactionAsync(Guid userId, WalletTransactionRequest request,
        WalletTransactionType type, Guid performedByUserId, CancellationToken cancellationToken = default)
    {
        ValidateAmount(request.Amount);
        if (type is not (WalletTransactionType.Deposit or WalletTransactionType.Withdrawal))
            throw new ArgumentException("Invalid wallet transaction type.");
        await EnsureActiveUserAsync(userId, "Wallet user", cancellationToken);
        await EnsureActiveUserAsync(performedByUserId, "Performing user", cancellationToken);
        if (!await repository.UserHasRoleAsync(performedByUserId, AdminRole, cancellationToken))
            throw new UnauthorizedAccessException("Only an Admin can perform wallet operations.");

        await repository.GetOrCreateWalletByUserIdAsync(userId, 1, 1, cancellationToken);
        var isDeposit = type == WalletTransactionType.Deposit;
        var financial = new FinancialTransaction
        {
            Amount = request.Amount,
            TransactionType = isDeposit ? TransactionType.WalletDeposit : TransactionType.WalletWithdrawal,
            Direction = isDeposit ? TransactionDirection.Credit : TransactionDirection.Debit,
            Status = FinancialTransactionStatus.Completed,
            ReferenceType = FinancialReferenceTypes.Wallet,
            Description = Normalize(request.Description),
            CreatedByUserId = performedByUserId
        };
        var walletTransaction = new WalletTransaction
        {
            Amount = request.Amount,
            Type = type,
            Description = Normalize(request.Description)
        };
        await repository.AddWalletTransactionAsync(userId, financial, walletTransaction, cancellationToken);
        var result = await repository.GetOrCreateWalletByUserIdAsync(userId, 1, 20, cancellationToken);
        return Map(result.Wallet, result.TotalCount, 1, 20);
    }

    private async Task EnsureActiveUserAsync(Guid userId, string label, CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty || !await repository.UserExistsAsync(userId, cancellationToken))
            throw new KeyNotFoundException($"{label} was not found or is inactive.");
    }
    private static void ValidateAmount(decimal amount)
    {
        if (amount <= 0) throw new ArgumentException("Amount must be greater than zero.");
        if (decimal.Round(amount, 2) != amount) throw new ArgumentException("Amount cannot have more than two decimal places.");
    }
    private static void ValidatePagination(int page, int pageSize)
    {
        if (page < 1) throw new ArgumentException("Page must be at least 1.");
        if (pageSize is < 1 or > MaximumPageSize) throw new ArgumentException($"Page size must be between 1 and {MaximumPageSize}.");
    }
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static FinancialTransactionDto Map(FinancialTransaction x) => new(x.Id, x.Amount, x.TransactionType,
        x.Direction, x.Status, x.ReferenceType, x.ReferenceId, x.Description, x.CreatedAt, x.CreatedByUserId,
        x.UpdatedAt, x.UpdatedByUserId);
    private static WalletDto Map(Wallet x, int totalCount, int page, int pageSize) => new(x.Id, x.UserId, x.Balance,
        x.CreatedAt, x.IsActive, x.Transactions.Select(t => new WalletTransactionDto(t.Id,
            t.FinancialTransactionId, t.Amount, t.Type, t.CreatedAt, t.Description)).ToArray(), totalCount, page, pageSize);
}
