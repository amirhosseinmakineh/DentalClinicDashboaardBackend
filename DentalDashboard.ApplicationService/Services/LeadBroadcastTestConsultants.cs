namespace DentalDashboard.ApplicationService.Services;

using DentalDashboard.Domain.Models;

/// <summary>
/// Temporary hardcoded consultants for lead-broadcast sound testing.
/// Remove or empty <see cref="UserIds"/> before production rollout.
/// </summary>
internal static class LeadBroadcastTestConsultants
{
    internal static readonly Guid[] UserIds =
    [
        Guid.Parse("9B19E167-18F3-4BCC-B10C-706E985A620E"),
        Guid.Parse("E9F685AC-140F-4B0B-ADDB-DBBF407EE669"),
    ];

    internal static bool IsEnabled => UserIds.Length > 0;

    internal static bool IsAllowed(Guid userId) =>
        !IsEnabled || UserIds.Contains(userId);

    internal static IEnumerable<ConsultantProfile> Filter(IEnumerable<ConsultantProfile> consultants) =>
        IsEnabled ? consultants.Where(x => UserIds.Contains(x.UserId)) : consultants;

}
