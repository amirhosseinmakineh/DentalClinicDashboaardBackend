using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.Models;
using DentalDashboard.Infrastracture.Context;
using DentalDashboard.Utilities.Time;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Infrastracture.Services;

public sealed class SecretaryAccessService : ISecretaryAccessService
{
    private readonly DentalContext context;
    public SecretaryAccessService(DentalContext context) => this.context = context;

    public async Task<SecretaryAccess> GetAccessAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await context.Users.AsNoTracking().Where(x => x.Id == userId && x.IsActive && !x.IsDeleted)
            .Select(x => new { x.SecretaryType, IsSecretary = x.UserRoles.Any(ur => !ur.IsDeleted && ur.Role.RoleName.ToLower() == "secretary") })
            .FirstOrDefaultAsync(cancellationToken);
        if (user == null || !user.IsSecretary) return new(false, null, [], []);
        var type = user.SecretaryType ?? SecretaryType.Main;
        if (type == SecretaryType.Main)
            return new(true, type, Enum.GetValues<DayOfWeek>(), Enum.GetValues<SecretaryPermissionType>());

        var days = await context.SecretaryAccessSchedules.AsNoTracking()
            .Where(x => x.UserId == userId && x.IsActive && !x.IsDeleted).Select(x => x.DayOfWeek)
            .Distinct().OrderBy(x => x).ToListAsync(cancellationToken);
        var today = IranTimeHelper.IranLocalNow.DayOfWeek;
        var permissions = await context.SecretaryAccessPermissions.AsNoTracking()
            .Where(x => x.SecretaryUserId == userId && x.DayOfWeek == today && x.IsActive && !x.IsDeleted)
            .Select(x => x.PermissionType).Distinct().OrderBy(x => x).ToListAsync(cancellationToken);
        return new(true, type, days, permissions);
    }

    public async Task<bool> HasPermissionAsync(Guid userId, SecretaryPermissionType permission, CancellationToken cancellationToken = default)
    {
        var access = await GetAccessAsync(userId, cancellationToken);
        if (access.HasFullAccess) return true;
        var today = IranTimeHelper.IranLocalNow.DayOfWeek;
        return access.IsSecretary && access.AllowedDays.Contains(today) && access.Permissions.Contains(permission);
    }

    public async Task<bool> CanAccessReservationAsync(Guid userId, long reservationId, CancellationToken cancellationToken = default)
    {
        var access = await GetAccessAsync(userId, cancellationToken);
        if (access.HasFullAccess) return true;
        if (!access.IsSecretary || !access.AllowedDays.Contains(IranTimeHelper.IranLocalNow.DayOfWeek)) return false;
        return await context.Reservations.AsNoTracking().AnyAsync(x => x.Id == reservationId && !x.IsDeleted, cancellationToken);
    }

    public async Task<IReadOnlyCollection<DayOfWeek>> GetScheduleAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await context.SecretaryAccessSchedules.AsNoTracking().Where(x => x.UserId == userId && x.IsActive && !x.IsDeleted)
            .Select(x => x.DayOfWeek).Distinct().OrderBy(x => x).ToListAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<DayOfWeek, IReadOnlyCollection<SecretaryPermissionType>>> GetPermissionScheduleAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        var rows = await context.SecretaryAccessPermissions.AsNoTracking()
            .Where(x => x.SecretaryUserId == userId && x.IsActive && !x.IsDeleted)
            .Select(x => new { x.DayOfWeek, x.PermissionType }).ToListAsync(cancellationToken);
        return rows.GroupBy(x => x.DayOfWeek).ToDictionary(x => x.Key,
            x => (IReadOnlyCollection<SecretaryPermissionType>)x.Select(y => y.PermissionType).Distinct().OrderBy(y => y).ToArray());
    }

    public async Task<ScheduleUpdateResult> UpdateScheduleAsync(Guid userId, SecretaryType secretaryType,
        IReadOnlyDictionary<DayOfWeek, IReadOnlyCollection<SecretaryPermissionType>> dayPermissions,
        Guid changedByUserId, CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(secretaryType) || dayPermissions.Keys.Any(x => !Enum.IsDefined(x)) ||
            dayPermissions.Values.SelectMany(x => x).Any(x => !Enum.IsDefined(x)))
            return new(false, "روز یا دسترسی نامعتبر است");
        if (secretaryType == SecretaryType.Main && dayPermissions.Count > 0)
            return new(false, "منشی اصلی نیاز به برنامه دسترسی ندارد");

        var user = await context.Users.Include(x => x.UserRoles).ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == userId && !x.IsDeleted, cancellationToken);
        if (user == null || !user.UserRoles.Any(x => !x.IsDeleted && x.Role.RoleName.ToLower() == "secretary"))
            return new(false, "کاربر باید نقش منشی داشته باشد");

        var schedules = await context.SecretaryAccessSchedules.Where(x => x.UserId == userId).ToListAsync(cancellationToken);
        var permissions = await context.SecretaryAccessPermissions.Where(x => x.SecretaryUserId == userId).ToListAsync(cancellationToken);
        var oldDays = schedules.Where(x => x.IsActive && !x.IsDeleted).Select(x => x.DayOfWeek).Distinct().OrderBy(x => x).ToArray();
        context.SecretaryAccessSchedules.RemoveRange(schedules);
        context.SecretaryAccessPermissions.RemoveRange(permissions);

        if (secretaryType == SecretaryType.Assistant)
        {
            await context.SecretaryAccessSchedules.AddRangeAsync(dayPermissions.Keys.Select(day => new SecretaryAccessSchedule
                { UserId = userId, DayOfWeek = day, IsActive = true }), cancellationToken);
            await context.SecretaryAccessPermissions.AddRangeAsync(dayPermissions.SelectMany(entry => entry.Value.Distinct().Select(permission =>
                new SecretaryAccessPermission { SecretaryUserId = userId, DayOfWeek = entry.Key, PermissionType = permission, IsActive = true })), cancellationToken);
        }
        user.SecretaryType = secretaryType;
        user.UpdatedAt = DateTime.UtcNow;
        await context.SecretaryAccessScheduleAudits.AddAsync(new SecretaryAccessScheduleAudit
        {
            Id = Guid.NewGuid(), SecretaryUserId = userId, ChangedByUserId = changedByUserId,
            OldDays = string.Join(',', oldDays), NewDays = string.Join(',', dayPermissions.Keys.OrderBy(x => x))
        }, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return new(true);
    }
}
