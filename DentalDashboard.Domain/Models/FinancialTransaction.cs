using DentalDashboard.Domain.Enums;
using DentalDashboard.Framwork.Domain;

namespace DentalDashboard.Domain.Models;

public class FinancialTransaction : BaseEntity<long>
{
    public decimal Amount { get; set; }
    public TransactionType TransactionType { get; set; }
    public string? ReferenceType { get; set; }
    public long? ReferenceId { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid CreatedByUserId { get; set; }

    public User CreatedByUser { get; set; } = default!;
    public ICollection<WalletTransaction> WalletTransactions { get; set; } = new HashSet<WalletTransaction>();
}
