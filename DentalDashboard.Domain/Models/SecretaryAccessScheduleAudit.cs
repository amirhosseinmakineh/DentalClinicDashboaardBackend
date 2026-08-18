namespace DentalDashboard.Domain.Models;

public class SecretaryAccessScheduleAudit : BaseAuditableEntity<Guid>
{
    public Guid SecretaryUserId { get; set; }
    public Guid ChangedByUserId { get; set; }
    public string OldDays { get; set; } = string.Empty;
    public string NewDays { get; set; } = string.Empty;
}
