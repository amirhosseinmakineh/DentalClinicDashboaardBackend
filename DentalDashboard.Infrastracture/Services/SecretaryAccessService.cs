using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Models;
using DentalDashboard.Infrastracture.Context;
using DentalDashboard.Utilities.Time;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Infrastracture.Services;

public sealed class SecretaryAccessService : ISecretaryAccessService
{
    private readonly DentalContext context;
    private readonly IUnitOfWork unitOfWork;
    public SecretaryAccessService(DentalContext context, IUnitOfWork unitOfWork)
    {
        this.context = context;
        this.unitOfWork = unitOfWork;
    }

    public async Task<SecretaryAccessDto> GetAccessAsync(Guid userId,CancellationToken cancellationToken = default)
    {
        var user = await context.Users
            .AsNoTracking()
            .Where(x =>
                x.Id == userId &&
                x.IsActive &&
                !x.IsDeleted)
            .Select(x => new
            {
                x.SecretaryType,

                IsSecretary = x.UserRoles.Any(ur =>
                    !ur.IsDeleted &&
                    ur.Role.RoleName.ToLower() == "secretary")
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null || !user.IsSecretary)
        {
            return new SecretaryAccessDto(false,null,[],[]);
        }

        if (user.SecretaryType is null)
        {
            return new SecretaryAccessDto(true,null,[],[]);
        }

        var type = user.SecretaryType.Value;

        var today = IranTimeHelper.IranLocalNow.DayOfWeek;

        var allowedDays = await context.SecretaryAccessSchedules
            .AsNoTracking()
            .Where(x =>
                x.UserId == userId &&
                x.IsActive &&
                !x.IsDeleted)
            .Select(x => x.DayOfWeek)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);

        var permissions = await context.SecretaryAccessPermissions
            .AsNoTracking()
            .Where(x =>
                x.SecretaryUserId == userId &&
                x.DayOfWeek == today &&
                x.IsActive &&
                !x.IsDeleted)
            .Select(x => x.PermissionType)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);

        return new SecretaryAccessDto(
            true,
            type,
            allowedDays,
            permissions);
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
        await context.SecretaryAccessSchedules.
        AsNoTracking().
        Where(x => x.UserId == userId && x.IsActive && !x.IsDeleted)
            .Select(x => x.DayOfWeek)
        .Distinct()
        .OrderBy(x => x)
        .ToListAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<DayOfWeek, IReadOnlyCollection<SecretaryPermissionType>>>GetPermissionScheduleAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
    {
        var permissions = await context.SecretaryAccessPermissions
            .AsNoTracking()
            .Where(x =>
                x.SecretaryUserId == userId &&
                x.IsActive &&
                !x.IsDeleted)
            .Select(x => new
            {
                x.DayOfWeek,
                x.PermissionType
            })
            .ToListAsync(cancellationToken);

        var groupedPermissions = permissions
            .GroupBy(x => x.DayOfWeek);

        var result = new Dictionary<DayOfWeek,IReadOnlyCollection<SecretaryPermissionType>>();

        foreach (var group in groupedPermissions)
        {
            var dayPermissions = group
                .Select(x => x.PermissionType)
                .Distinct()
                .OrderBy(x => x)
                .ToArray();

            result.Add(group.Key,dayPermissions);
        }

        return result;
    }

    public async Task<ScheduleUpdateResult> UpdateScheduleAsync(Guid userId, SecretaryType secretaryType,
        IReadOnlyDictionary<DayOfWeek, IReadOnlyCollection<SecretaryPermissionType>> dayPermissions,
        Guid changedByUserId, CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(secretaryType) || 
            dayPermissions.Keys.Any(x => !Enum.IsDefined(x)) ||
            dayPermissions.Values.SelectMany(x => x).Any(x => !Enum.IsDefined(x)))
            return new(false, "روز یا دسترسی نامعتبر است");

        var user = await context.Users.
            Include(x => x.UserRoles).ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == userId && !x.IsDeleted, cancellationToken);

        if (user == null || !user.UserRoles.Any(x => !x.IsDeleted && x.Role.RoleName.ToLower() == "secretary"))
            return new(false, "کاربر باید نقش منشی داشته باشد");

        var schedules = await context.SecretaryAccessSchedules
            .Where(x => x.UserId == userId).ToListAsync(cancellationToken);

        var permissions = await context.SecretaryAccessPermissions
            .Where(x => x.SecretaryUserId == userId).ToListAsync(cancellationToken);

        var oldDays = schedules
            .Where(x => x.IsActive && !x.IsDeleted)
            .Select(x => x.DayOfWeek)
            .Distinct()
            .OrderBy(x => x)
            .ToArray();

        await unitOfWork.BeginTransactionAsync();
        context.SecretaryAccessSchedules.RemoveRange(schedules);
        context.SecretaryAccessPermissions.RemoveRange(permissions);

        if (schedules.Count > 0 || permissions.Count > 0)
            await context.SaveChangesAsync(cancellationToken);

        if (secretaryType == SecretaryType.Assistant)
        {
            var newSchedules = dayPermissions.Keys
                .Select(day => new SecretaryAccessSchedule
                { 
                    UserId = userId, 
                    DayOfWeek = day,
                    IsActive = true })
                .ToArray();
            var newPermissions = dayPermissions
                .SelectMany(entry => entry.Value.
                Distinct()
                .Select(permission =>
                new SecretaryAccessPermission
                {
                    SecretaryUserId = userId,
                    DayOfWeek = entry.Key,
                    PermissionType = permission,
                    IsActive = true
                })).ToArray();

            await context.SecretaryAccessSchedules
                .AddRangeAsync(newSchedules, cancellationToken);
            await context.SecretaryAccessPermissions
                .AddRangeAsync(newPermissions, cancellationToken);
        }
        user.SecretaryType = secretaryType;
        user.UpdatedAt = DateTime.UtcNow;
        await context.SecretaryAccessScheduleAudits.AddAsync(new SecretaryAccessScheduleAudit
        {
            Id = Guid.NewGuid(), SecretaryUserId = userId, ChangedByUserId = changedByUserId,
            OldDays = string.Join(',', oldDays), NewDays = string.Join(',', dayPermissions.Keys.OrderBy(x => x))
        }, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await unitOfWork.CommitAsync();
        return new(true);
    }
}
