using DentalDashboard.Domain.Enums;

namespace DentalDashboard.Domain.Models;

public class LeadAssignmentSetting : BaseAuditableEntity<long>
{
    public const long SingletonId = 1;

    public LeadAssignmentSourceType AssignmentSourceType { get; set; } = LeadAssignmentSourceType.NewLeads;
    public Guid? UpdatedByAdminId { get; set; }
    public User? UpdatedByAdmin { get; set; }
}
