using DentalDashboard.Domain.Secretary.Accountant.SecretarySales.Entities;

namespace DentalDashboard.Domain.Models;

public sealed class LeadRequestedService
{
    public long LeadAssignmentId { get; set; }
    public long ServiceId { get; set; }
    public LeadAssignment LeadAssignment { get; set; } = null!;
    public SecretarySaleService Service { get; set; } = null!;
}
