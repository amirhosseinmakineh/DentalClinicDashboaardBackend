using DentalDashboard.Framwork.Domain;

namespace DentalDashboard.Domain.Models;

public class Wallet : BaseEntity<long>
{
    public Guid UserId { get; set; }
    public decimal Balance { get; private set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    public User User { get; set; } = default!;
    public ICollection<WalletTransaction> Transactions { get; set; } = new HashSet<WalletTransaction>();
}
