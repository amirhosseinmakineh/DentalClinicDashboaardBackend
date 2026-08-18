using DentalDashboard.ApplicationService.Contract.Dtos.Financial;
using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Models;

namespace DentalDashboard.ApplicationService.Services;

public class FinancialTransactionService : IFinancialTransactionService
{
    private readonly IFinancialTransactionRepository repository;
    public FinancialTransactionService(IFinancialTransactionRepository repository) => this.repository = repository;

    public async Task<FinancialTransactionDto> CreateTransactionAsync(CreateFinancialTransactionRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateAmount(request.Amount);
        if (!await repository.UserExistsAsync(request.CreatedByUserId, cancellationToken))
            throw new KeyNotFoundException("Creating user was not found or is inactive.");
        var entity = await repository.AddAsync(new FinancialTransaction
        {
            Amount = request.Amount,
            TransactionType = request.TransactionType,
            ReferenceType = Normalize(request.ReferenceType),
            ReferenceId = request.ReferenceId,
            Description = Normalize(request.Description),
            CreatedByUserId = request.CreatedByUserId
        }, cancellationToken);
        return Map(entity);
    }

    public async Task<FinancialTransactionDto?> GetTransactionAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0) throw new ArgumentException("Transaction id must be positive.");
        var entity = await repository.GetByIdAsync(id, cancellationToken);
        return entity is null ? null : Map(entity);
    }

    public async Task<WalletDto> GetUserWalletAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (!await repository.UserExistsAsync(userId, cancellationToken))
            throw new KeyNotFoundException("User was not found or is inactive.");
        var wallet = await repository.GetWalletByUserIdAsync(userId, cancellationToken);
        if (wallet is null) throw new KeyNotFoundException("Wallet was not found.");
        return Map(wallet);
    }

    public async Task<WalletDto> AddWalletTransactionAsync(Guid userId, WalletTransactionRequest request,
        WalletTransactionType type, CancellationToken cancellationToken = default)
    {
        ValidateAmount(request.Amount);
        if (!Enum.IsDefined(type)) throw new ArgumentException("Invalid wallet transaction type.");
        if (!await repository.UserExistsAsync(userId, cancellationToken))
            throw new KeyNotFoundException("Wallet user was not found or is inactive.");
        if (!await repository.UserExistsAsync(request.CreatedByUserId, cancellationToken))
            throw new KeyNotFoundException("Creating user was not found or is inactive.");

        var financial = new FinancialTransaction
        {
            Amount = request.Amount,
            TransactionType = type == WalletTransactionType.Deposit
                ? TransactionType.WalletDeposit : TransactionType.WalletWithdrawal,
            ReferenceType = nameof(Wallet),
            Description = Normalize(request.Description),
            CreatedByUserId = request.CreatedByUserId
        };
        var walletTransaction = new WalletTransaction
        {
            Amount = request.Amount,
            Type = type,
            Description = Normalize(request.Description)
        };
        var wallet = await repository.AddWalletTransactionAsync(userId, financial, walletTransaction, cancellationToken);
        return Map(wallet);
    }

    private static void ValidateAmount(decimal amount)
    {
        if (amount <= 0) throw new ArgumentException("Amount must be greater than zero.");
        if (decimal.Round(amount, 2) != amount) throw new ArgumentException("Amount cannot have more than two decimal places.");
    }
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static FinancialTransactionDto Map(FinancialTransaction x) => new(x.Id, x.Amount, x.TransactionType,
        x.ReferenceType, x.ReferenceId, x.Description, x.CreatedAt, x.CreatedByUserId);
    private static WalletDto Map(Wallet x) => new(x.Id, x.UserId, x.Balance, x.CreatedAt, x.IsActive,
        x.Transactions.OrderByDescending(t => t.CreatedAt).Select(t => new WalletTransactionDto(t.Id,
            t.FinancialTransactionId, t.Amount, t.Type, t.CreatedAt, t.Description)).ToArray());
}
