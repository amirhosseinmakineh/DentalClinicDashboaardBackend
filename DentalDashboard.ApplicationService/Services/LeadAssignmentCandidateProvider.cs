using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IRepositories;

namespace DentalDashboard.ApplicationService.Services;

public sealed class LeadAssignmentCandidateProvider(
    ILeadAssignmentRepository leads,
    ILeadAssignmentSettingRepository settings) : ILeadAssignmentCandidateProvider
{
    public async Task<LeadAssignmentCandidateBatch> GetCurrentForDispatchAsync(
        TimeSpan redispatchInterval,
        CancellationToken cancellationToken = default)
    {
        var sourceType = await GetSourceTypeAsync(cancellationToken);
        var burned = sourceType == LeadAssignmentSourceType.BurnedLeads;
        var lead = burned ? await leads.GetCurrentBurnedLeadForDispatchAsync(redispatchInterval) : await leads.GetCurrentRealtimeLeadForDispatchAsync(redispatchInterval);
        return new LeadAssignmentCandidateBatch(sourceType, lead);
    }

    public async Task<LeadAssignmentCandidateBatch> GetActiveAsync(
        CancellationToken cancellationToken = default)
    {
        var sourceType = await GetSourceTypeAsync(cancellationToken);
        var burned = sourceType == LeadAssignmentSourceType.BurnedLeads;
        var lead = burned ? await leads.GetActiveBurnedLeadAsync() : await leads.GetActiveRealtimeBroadcastLeadAsync();
        return new LeadAssignmentCandidateBatch(sourceType, lead);
    }

    private async Task<LeadAssignmentSourceType> GetSourceTypeAsync(CancellationToken cancellationToken)
    {
        var setting = await settings.GetCurrentAsync(cancellationToken);
        return setting is not null && Enum.IsDefined(setting.AssignmentSourceType)
            ? setting.AssignmentSourceType
            : LeadAssignmentSourceType.NewLeads;
    }
}
