using DentalDashboard.Domain.Enums;

namespace DentalDashboard.ApplicationService.Contract.IServices;

public interface ISecretaryAccessService
{
    Task<SecretaryAccess> GetAccessAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> CanAccessReservationAsync(Guid userId, long reservationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<DayOfWeek>> GetScheduleAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ScheduleUpdateResult> UpdateScheduleAsync(Guid userId, SecretaryType secretaryType,
        IReadOnlyCollection<DayOfWeek> days, Guid changedByUserId, CancellationToken cancellationToken = default);
}

public sealed record SecretaryAccess(bool IsSecretary, SecretaryType? Type, IReadOnlyCollection<DayOfWeek> AllowedDays)
{
    public bool HasFullAccess => IsSecretary && Type == SecretaryType.Main;
}

public sealed record ScheduleUpdateResult(bool Succeeded, string? Error = null);
