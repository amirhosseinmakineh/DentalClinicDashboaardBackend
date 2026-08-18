using DentalDashboard.ApplicationService.Contract.Dtos.Financial;
using DentalDashboard.ApplicationService.Services;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Models;

namespace DentalDashboard.Tests;

public class FinancialTransactionServiceTests
{
    [Fact]
    public async Task AdminCanDepositAndCreatesFinancialHistory()
    {
        var repository = new FakeFinancialRepository();
        var adminId = repository.AddUser("Admin");
        var walletUserId = repository.AddUser("NormalUser");
        var service = new FinancialTransactionService(repository);

        var wallet = await service.AddWalletTransactionAsync(walletUserId, new(1_000),
            WalletTransactionType.Deposit, adminId);

        Assert.Equal(1_000, wallet.Balance);
        Assert.Single(repository.FinancialTransactions);
        Assert.Single(repository.WalletTransactions);
        Assert.Equal(TransactionDirection.Credit, repository.FinancialTransactions[0].Direction);
    }

    [Theory]
    [InlineData("Secretary")]
    [InlineData("Consultant")]
    public async Task NonAdminCannotDeposit(string role)
    {
        var repository = new FakeFinancialRepository();
        var actorId = repository.AddUser(role);
        var walletUserId = repository.AddUser("NormalUser");
        var service = new FinancialTransactionService(repository);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.AddWalletTransactionAsync(
            walletUserId, new(100), WalletTransactionType.Deposit, actorId));
    }

    [Fact]
    public async Task CannotWithdrawMoreThanBalance()
    {
        var repository = new FakeFinancialRepository();
        var adminId = repository.AddUser("Admin");
        var walletUserId = repository.AddUser("NormalUser");
        var service = new FinancialTransactionService(repository);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.AddWalletTransactionAsync(
            walletUserId, new(100), WalletTransactionType.Withdrawal, adminId));
    }

    [Fact]
    public async Task CannotWithdrawFromInactiveWallet()
    {
        var repository = new FakeFinancialRepository();
        var adminId = repository.AddUser("Admin");
        var walletUserId = repository.AddUser("NormalUser");
        repository.InactiveUsers.Add(walletUserId);
        var service = new FinancialTransactionService(repository);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.AddWalletTransactionAsync(
            walletUserId, new(100), WalletTransactionType.Withdrawal, adminId));
    }

    [Fact]
    public async Task CannotCreateNegativeFinancialTransaction()
    {
        var repository = new FakeFinancialRepository();
        var adminId = repository.AddUser("Admin");
        var service = new FinancialTransactionService(repository);
        var request = new CreateFinancialTransactionRequest(-1, TransactionType.Expense, TransactionDirection.Debit);

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateTransactionAsync(request, adminId));
    }

    [Fact]
    public async Task TransactionHistoryRemainsAfterMultipleBalanceChanges()
    {
        var repository = new FakeFinancialRepository();
        var adminId = repository.AddUser("Admin");
        var walletUserId = repository.AddUser("Consultant");
        var service = new FinancialTransactionService(repository);

        await service.AddWalletTransactionAsync(walletUserId, new(500), WalletTransactionType.Deposit, adminId);
        var wallet = await service.AddWalletTransactionAsync(walletUserId, new(200), WalletTransactionType.Withdrawal, adminId);

        Assert.Equal(300, wallet.Balance);
        Assert.Equal(2, wallet.TotalCount);
        Assert.Equal(2, repository.FinancialTransactions.Count);
        Assert.Equal(2, repository.WalletTransactions.Count);
    }

    private sealed class FakeFinancialRepository : IFinancialTransactionRepository
    {
        private readonly Dictionary<Guid, string> users = [];
        private readonly Dictionary<Guid, Wallet> wallets = [];
        private readonly Dictionary<Guid, decimal> balances = [];
        public HashSet<Guid> InactiveUsers { get; } = [];
        public List<FinancialTransaction> FinancialTransactions { get; } = [];
        public List<WalletTransaction> WalletTransactions { get; } = [];

        public Guid AddUser(string role)
        {
            var id = Guid.NewGuid();
            users[id] = role;
            return id;
        }
        public Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(users.ContainsKey(userId));
        public Task<bool> UserHasRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken = default) =>
            Task.FromResult(users.TryGetValue(userId, out var role) && role == roleName);
        public Task<FinancialTransaction?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
            Task.FromResult(FinancialTransactions.SingleOrDefault(x => x.Id == id));
        public Task<FinancialTransaction> AddAsync(FinancialTransaction transaction, CancellationToken cancellationToken = default)
        {
            transaction.Id = FinancialTransactions.Count + 1;
            FinancialTransactions.Add(transaction);
            return Task.FromResult(transaction);
        }
        public Task<(Wallet Wallet, int TotalCount)> GetOrCreateWalletByUserIdAsync(Guid userId, int page, int pageSize,
            CancellationToken cancellationToken = default)
        {
            if (!wallets.TryGetValue(userId, out var wallet))
            {
                wallet = new Wallet { Id = wallets.Count + 1, UserId = userId };
                wallets[userId] = wallet;
                balances[userId] = 0;
            }
            SetBalance(wallet, balances[userId]);
            wallet.IsActive = !InactiveUsers.Contains(userId);
            wallet.Transactions = WalletTransactions.Where(x => x.WalletId == wallet.Id)
                .OrderByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return Task.FromResult((wallet, WalletTransactions.Count(x => x.WalletId == wallet.Id)));
        }
        public Task<Wallet> AddWalletTransactionAsync(Guid userId, FinancialTransaction transaction,
            WalletTransaction walletTransaction, CancellationToken cancellationToken = default)
        {
            var wallet = wallets[userId];
            if (!wallet.IsActive) throw new InvalidOperationException("Wallet is inactive.");
            if (walletTransaction.Type == WalletTransactionType.Withdrawal && balances[userId] < walletTransaction.Amount)
                throw new InvalidOperationException("Withdrawal amount cannot exceed wallet balance.");
            transaction.Id = FinancialTransactions.Count + 1;
            walletTransaction.Id = WalletTransactions.Count + 1;
            walletTransaction.WalletId = wallet.Id;
            walletTransaction.FinancialTransactionId = transaction.Id;
            FinancialTransactions.Add(transaction);
            WalletTransactions.Add(walletTransaction);
            balances[userId] += walletTransaction.Type == WalletTransactionType.Deposit
                ? walletTransaction.Amount : -walletTransaction.Amount;
            SetBalance(wallet, balances[userId]);
            return Task.FromResult(wallet);
        }
        private static void SetBalance(Wallet wallet, decimal balance) =>
            typeof(Wallet).GetProperty(nameof(Wallet.Balance))!.SetValue(wallet, balance);
    }
}
