using DentalDashboard.Domain.Enums;

namespace DentalDashboard.ApplicationService.Contract.IServices;

public interface IUserPresenceService
{
    Task LogAsync(
        Guid userId,
        UserPresenceEventType eventType,
        DateTime? occurredAt = null,
        string? description = null,
        CancellationToken cancellationToken = default);
}
