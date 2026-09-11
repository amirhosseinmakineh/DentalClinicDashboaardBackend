using DentalDashboard.Domain.Enums;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using System.Text.Json.Serialization;

namespace DentalDashboard.ApplicationService.Contract.Requests.Consultant.Commands;

public class CloseLeadCommand : ICommand<object>
{
    public long LeadAssignmentId { get; set; }
    public LeadClosureReason Reason { get; set; }
    public string? Description { get; set; }

    [JsonIgnore]
    public long ConsultantProfileId { get; set; }
}
