using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Services;

public sealed class LeadAssignmentCandidateProvider(
    ILeadAssignmentRepository leads,
    ILeadAssignmentSettingRepository settings,
    IConsultantProfileRepository consultants) : ILeadAssignmentCandidateProvider
{
    public async Task<LeadAssignmentCandidateBatch> GetCurrentForDispatchAsync(
        LeadAssignmentSourceType sourceType,
        TimeSpan redispatchInterval,
        CancellationToken cancellationToken = default)
    {
        var burned = sourceType == LeadAssignmentSourceType.BurnedLeads;
        var lead = burned ? await leads.GetCurrentBurnedLeadForDispatchAsync(redispatchInterval) : await leads.GetCurrentRealtimeLeadForDispatchAsync(redispatchInterval);
        return new LeadAssignmentCandidateBatch(sourceType, lead);
    }

    public async Task<LeadAssignmentCandidateBatch> GetActiveAsync(
        long consultantProfileId,
        CancellationToken cancellationToken = default)
    {
        var sourceType = await GetSourceTypeAsync(consultantProfileId, cancellationToken);
        var burned = sourceType == LeadAssignmentSourceType.BurnedLeads;
        var lead = burned ? await leads.GetActiveBurnedLeadAsync() : await leads.GetActiveRealtimeBroadcastLeadAsync();
        return new LeadAssignmentCandidateBatch(sourceType, lead);
    }

    public async Task<LeadAssignmentSourceType> GetSourceTypeAsync(
        long consultantProfileId,
        CancellationToken cancellationToken = default)
    {
        var preferred = await consultants.GetAll()
            .AsNoTracking()
            .Where(profile => profile.Id == consultantProfileId && !profile.IsDeleted)
            .Select(profile => profile.PreferredLeadSourceType)
            .SingleOrDefaultAsync(cancellationToken);
        if (preferred.HasValue && Enum.IsDefined(preferred.Value))
            return preferred.Value;

        var setting = await settings.GetCurrentAsync(cancellationToken);
        return setting is not null && Enum.IsDefined(setting.AssignmentSourceType)
            ? setting.AssignmentSourceType
            : LeadAssignmentSourceType.NewLeads;
    }
}
