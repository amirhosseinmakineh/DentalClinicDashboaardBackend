using DentalDashboard.Domain.Models;
using DentalDashboard.Domain.Secretary.Accountant.SecretarySales.Enums;
using DentalDashboard.Framwork.Domain;

namespace DentalDashboard.Domain.Secretary.Accountant.SecretarySales.Entities;

public sealed class SecretarySaleService : BaseAuditableEntity<long>
{
    public string Title { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal SecretaryReward { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<SecretarySale> Sales { get; set; } = [];
}

public sealed class SecretarySale : BaseAuditableEntity<long>
{
    public Guid SecretaryUserId { get; set; }
    public Guid PatientUserId { get; set; }
    public long ServiceId { get; set; }
    public decimal SalePrice { get; set; }
    public decimal SecretaryReward { get; set; }
    public SecretarySaleStatus Status { get; set; } = SecretarySaleStatus.PendingAdminApproval;
    public Guid? ReviewedByAdminId { get; set; }
    public DateTime? ReviewedAt { get; set; }

    public User SecretaryUser { get; set; } = null!;
    public User PatientUser { get; set; } = null!;
    public SecretarySaleService Service { get; set; } = null!;
    public User? ReviewedByAdmin { get; set; }
    public ICollection<SecretaryWalletTransaction> WalletTransactions { get; set; } = [];
}

public sealed class SecretaryWallet : BaseAuditableEntity<long>
{
    public Guid SecretaryUserId { get; set; }
    public decimal Balance { get; set; }
    public User SecretaryUser { get; set; } = null!;
    public ICollection<SecretaryWalletTransaction> Transactions { get; set; } = [];
}

public sealed class SecretaryWalletTransaction : BaseAuditableEntity<long>
{
    public long WalletId { get; set; }
    public Guid SecretaryUserId { get; set; }
    public long? SecretarySaleId { get; set; }
    public decimal Amount { get; set; }
    public SecretaryWalletTransactionType TransactionType { get; set; }
    public string Description { get; set; } = string.Empty;

    public SecretaryWallet Wallet { get; set; } = null!;
    public User SecretaryUser { get; set; } = null!;
    public SecretarySale? SecretarySale { get; set; }
}
