using DentalDashboard.Framwork.Domain;

namespace DentalDashboard.Accounting.Domain.SecretarySales.Entities;

public sealed class SecretarySaleService : BaseAuditableEntity<long>
{
    public string Title { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal SecretaryReward { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<SecretarySale> Sales { get; set; } = [];
}
