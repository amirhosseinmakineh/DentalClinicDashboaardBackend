using DentalDashboard.ApplicationService.Settings;
using DentalDashboard.Domain.Models;
using Microsoft.Extensions.Options;

namespace DentalDashboard.ApplicationService.Services;

public sealed class LeadBroadcastTestFilter(IOptions<LeadBroadcastSettings> options)
{
    private Guid[]? _parsedUserIds;

    public bool IsEnabled => GetUserIds().Length > 0;

    public bool IsAllowed(Guid userId) =>
        !IsEnabled || GetUserIds().Contains(userId);

    public IEnumerable<ConsultantProfile> FilterEligibleForBroadcast(
        IEnumerable<ConsultantProfile> consultants) =>
        IsEnabled
            ? consultants.Where(x => IsAllowed(x.UserId))
            : consultants;

    private Guid[] GetUserIds()
    {
        if (_parsedUserIds is not null)
            return _parsedUserIds;

        _parsedUserIds = options.Value.TestUserIds
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => Guid.TryParse(x.Trim(), out var id) ? id : Guid.Empty)
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToArray();

        return _parsedUserIds;
    }
}
