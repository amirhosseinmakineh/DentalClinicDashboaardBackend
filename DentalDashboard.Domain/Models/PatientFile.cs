using DentalDashboard.Domain.Enums;

namespace DentalDashboard.Domain.Models;

public sealed class PatientFile : BaseAuditableEntity<long>
{
    public long? PatientReferenceId { get; set; }
    public LeadAssignment? PatientReference { get; set; }
    public long FileNumber { get; set; }
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string PhoneNumber { get; set; } = default!;
    public string? Description { get; set; }
    public PatientFileSourceType SourceType { get; set; }
}
