namespace DentalDashboard.Domain.Models;

public class SecretaryAccessSchedule : BaseAuditableEntity<int>
{
    public Guid UserId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public bool IsActive { get; set; } = true;
    public User User { get; set; } = default!;
}
