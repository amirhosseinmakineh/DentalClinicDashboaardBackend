using DentalDashboard.Domain.Enums;
using DentalDashboard.Framwork.Domain;

namespace DentalDashboard.Domain.Models;

public class WalletTransaction : BaseEntity<long>
{
    public long WalletId { get; set; }
    public long FinancialTransactionId { get; set; }
    public decimal Amount { get; set; }
    public WalletTransactionType Type { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? Description { get; set; }

    public Wallet Wallet { get; set; } = default!;
    public FinancialTransaction FinancialTransaction { get; set; } = default!;
}
