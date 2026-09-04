using DentalDashboard.Domain.Enums;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;

namespace DentalDashboard.ApplicationService.Contract.Requests.Admin.LeadAssignmentSettings;

public sealed record LeadAssignmentSettingResponse
{
    public LeadAssignmentSourceType AssignmentSourceType { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public sealed record GetLeadAssignmentSettingQuery : IQuery<LeadAssignmentSettingResponse>;

public sealed class UpdateLeadAssignmentSettingCommand : ICommand<LeadAssignmentSettingResponse>
{
    public Guid AdminUserId { get; set; }
    public LeadAssignmentSourceType AssignmentSourceType { get; set; }
}
