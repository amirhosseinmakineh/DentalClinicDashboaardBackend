using DentalDashboard.Domain.Models;
using DentalDashboard.Accounting.Domain.SecretarySales.Enums;
using DentalDashboard.Framwork.Domain;

namespace DentalDashboard.Accounting.Domain.SecretarySales.Entities;

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
