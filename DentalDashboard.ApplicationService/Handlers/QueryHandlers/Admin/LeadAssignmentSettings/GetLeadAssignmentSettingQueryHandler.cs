using DentalDashboard.ApplicationService.Contract.Requests.Admin.LeadAssignmentSettings;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IRepositories;

namespace DentalDashboard.ApplicationService.Handlers.QueryHandlers.Admin.LeadAssignmentSettings;

public sealed class GetLeadAssignmentSettingQueryHandler(ILeadAssignmentSettingRepository settings)
    : IQueryHandler<GetLeadAssignmentSettingQuery, LeadAssignmentSettingResponse>
{
    public async Task<LeadAssignmentSettingResponse> HandleAsync(
        GetLeadAssignmentSettingQuery query,
        CancellationToken cancellationToken = default)
    {
        var setting = await settings.GetCurrentAsync(cancellationToken);
        return new LeadAssignmentSettingResponse
        {
            AssignmentSourceType = setting?.AssignmentSourceType ?? LeadAssignmentSourceType.NewLeads,
            UpdatedAt = setting?.UpdatedAt
        };
    }
}
