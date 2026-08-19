using DentalDashboard.Domain.Enums;

namespace DentalDashboard.ApplicationService.Contract.IServices;

public interface ISecretaryAccessService
{
    Task<SecretaryAccess> GetAccessAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> CanAccessReservationAsync(Guid userId, long reservationId, CancellationToken cancellationToken = default);
    Task<bool> HasPermissionAsync(Guid userId, SecretaryPermissionType permission, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<DayOfWeek>> GetScheduleAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<DayOfWeek, IReadOnlyCollection<SecretaryPermissionType>>> GetPermissionScheduleAsync(
        Guid userId, CancellationToken cancellationToken = default);
    Task<ScheduleUpdateResult> UpdateScheduleAsync(Guid userId, SecretaryType secretaryType,
        IReadOnlyDictionary<DayOfWeek, IReadOnlyCollection<SecretaryPermissionType>> dayPermissions,
        Guid changedByUserId, CancellationToken cancellationToken = default);
}

public sealed record SecretaryAccess(bool IsSecretary, SecretaryType? Type, IReadOnlyCollection<DayOfWeek> AllowedDays,
    IReadOnlyCollection<SecretaryPermissionType> Permissions)
{
    public bool HasFullAccess => IsSecretary && Type == SecretaryType.Main;
}

public sealed record ScheduleUpdateResult(bool Succeeded, string? Error = null);
