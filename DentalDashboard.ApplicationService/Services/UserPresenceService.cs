using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Models;

namespace DentalDashboard.ApplicationService.Services;

public class UserPresenceService : IUserPresenceService
{
    private readonly IUserPresenceLogRepository presenceLogRepository;

    public UserPresenceService(IUserPresenceLogRepository presenceLogRepository)
    {
        this.presenceLogRepository = presenceLogRepository;
    }

    public async Task LogAsync(
        Guid userId,
        UserPresenceEventType eventType,
        DateTime? occurredAt = null,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        var log = new UserPresenceLog
        {
            UserId = userId,
            EventType = eventType,
            OccurredAt = occurredAt ?? DateTime.Now,
            Description = description,
            CreatedAt = DateTime.UtcNow
        };

        await presenceLogRepository.AddAsync(log);
        await presenceLogRepository.SaveChange();
    }
}
