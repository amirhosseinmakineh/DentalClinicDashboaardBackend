using DentalDashboard.Domain.Enums;

namespace DentalDashboard.Domain.Models;

public class SecretaryAccessPermission : BaseAuditableEntity<int>
{
    public Guid SecretaryUserId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public SecretaryPermissionType PermissionType { get; set; }
    public bool IsActive { get; set; } = true;
    public User SecretaryUser { get; set; } = default!;
}
