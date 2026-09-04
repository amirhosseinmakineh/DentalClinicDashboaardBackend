using DentalDashboard.Domain.Models;
using DentalDashboard.Domain.Secretary.Accountant.SecretarySales.Enums;
using DentalDashboard.Framwork.Domain;

namespace DentalDashboard.Domain.Secretary.Accountant.SecretarySales.Entities;

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
