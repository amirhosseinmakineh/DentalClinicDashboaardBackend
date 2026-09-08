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

public sealed record ConsultantLeadAssignmentSettingResponse
{
    public long ConsultantProfileId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public bool IsOnline { get; init; }
    public LeadAssignmentSourceType? PreferredLeadSourceType { get; init; }
    public LeadAssignmentSourceType EffectiveLeadSourceType { get; init; }
}

public sealed record GetConsultantLeadAssignmentSettingsQuery
    : IQuery<IReadOnlyList<ConsultantLeadAssignmentSettingResponse>>;

public sealed class UpdateConsultantLeadAssignmentSettingCommand
    : ICommand<ConsultantLeadAssignmentSettingResponse>
{
    public Guid AdminUserId { get; set; }
    public long ConsultantProfileId { get; set; }
    public LeadAssignmentSourceType? PreferredLeadSourceType { get; set; }
}
