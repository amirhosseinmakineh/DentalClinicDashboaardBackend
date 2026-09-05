using DentalDashboard.Domain.Models;

namespace DentalDashboard.Accounting.Domain.SecretarySales.Entities;

public sealed class SecretaryWallet : BaseAuditableEntity<long>
{
    public Guid SecretaryUserId { get; set; }
    public decimal Balance { get; set; }
    public User SecretaryUser { get; set; } = null!;
    public ICollection<SecretaryWalletTransaction> Transactions { get; set; } = [];
}
