using DentalDashboard.ApplicationService.Settings;
using DentalDashboard.Domain.Models;
using Microsoft.Extensions.Options;

namespace DentalDashboard.ApplicationService.Services;

/// <summary>
/// Filters real-time broadcast visibility/notifications for test leads vs production leads.
/// Offline queue assignment is never filtered here.
/// </summary>
public sealed class LeadBroadcastTestFilter(IOptions<LeadBroadcastSettings> options)
{
    public bool IsTestModeEnabled => options.Value.EnableTestBroadcastLeads;

    public bool IsTestUser(Guid userId) =>
        LeadBroadcastTestData.IsTestUser(userId);

    public bool CanAccessLead(Guid userId, LeadAssignment lead)
    {
        if (!IsTestModeEnabled || !LeadBroadcastTestData.IsTestLead(lead))
            return true;

        return IsTestUser(userId);
    }

    public IEnumerable<ConsultantProfile> FilterForTestLeadPush(
        IEnumerable<ConsultantProfile> onlineConsultants,
        LeadAssignment lead)
    {
        if (!IsTestModeEnabled || !LeadBroadcastTestData.IsTestLead(lead))
            return onlineConsultants;

        return onlineConsultants.Where(x => IsTestUser(x.UserId));
    }
}
