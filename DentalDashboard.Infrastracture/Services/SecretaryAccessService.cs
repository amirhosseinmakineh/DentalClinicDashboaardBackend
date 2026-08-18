using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.Models;
using DentalDashboard.Infrastracture.Context;
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
        if (user == null || !user.IsSecretary) return new(false, null, Array.Empty<DayOfWeek>());
        var type = user.SecretaryType ?? SecretaryType.Main;
        if (type == SecretaryType.Main) return new(true, type, Array.Empty<DayOfWeek>());
        var days = await context.SecretaryAccessSchedules.AsNoTracking()
            .Where(x => x.UserId == userId && x.IsActive && !x.IsDeleted).Select(x => x.DayOfWeek).Distinct().ToListAsync(cancellationToken);
        return new(true, type, days);
    }

    public async Task<bool> CanAccessReservationAsync(Guid userId, long reservationId, CancellationToken cancellationToken = default)
    {
        var access = await GetAccessAsync(userId, cancellationToken);
        if (access.HasFullAccess) return true;
        if (!access.IsSecretary || access.AllowedDays.Count == 0) return false;
        var date = await context.Reservations.AsNoTracking().Where(x => x.Id == reservationId && !x.IsDeleted)
            .Select(x => (DateTime?)x.ReservationAt).FirstOrDefaultAsync(cancellationToken);
        return date.HasValue && access.AllowedDays.Contains(date.Value.DayOfWeek);
    }

    public async Task<IReadOnlyCollection<DayOfWeek>> GetScheduleAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await context.SecretaryAccessSchedules.AsNoTracking().Where(x => x.UserId == userId && x.IsActive && !x.IsDeleted)
            .Select(x => x.DayOfWeek).Distinct().OrderBy(x => x).ToListAsync(cancellationToken);

    public async Task<ScheduleUpdateResult> UpdateScheduleAsync(Guid userId, SecretaryType secretaryType,
        IReadOnlyCollection<DayOfWeek> days, Guid changedByUserId, CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(secretaryType) || days.Any(x => !Enum.IsDefined(x)) || days.Count != days.Distinct().Count())
            return new(false, "روزهای برنامه نامعتبر یا تکراری هستند");
        var user = await context.Users.Include(x => x.UserRoles).ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == userId && !x.IsDeleted, cancellationToken);
        if (user == null || !user.UserRoles.Any(x => !x.IsDeleted && x.Role.RoleName.ToLower() == "secretary"))
            return new(false, "کاربر باید نقش منشی داشته باشد");
        if (secretaryType == SecretaryType.Main && days.Count > 0)
            return new(false, "منشی اصلی نیاز به برنامه دسترسی ندارد");
        var existing = await context.SecretaryAccessSchedules.Where(x => x.UserId == userId).ToListAsync(cancellationToken);
        var oldDays = existing.Where(x => x.IsActive && !x.IsDeleted).Select(x => x.DayOfWeek).Distinct().OrderBy(x => x).ToArray();
        var requestedDays = secretaryType == SecretaryType.Assistant ? days.ToHashSet() : new HashSet<DayOfWeek>();
        context.SecretaryAccessSchedules.RemoveRange(existing.Where(x => !requestedDays.Contains(x.DayOfWeek)));
        foreach (var schedule in existing.Where(x => requestedDays.Contains(x.DayOfWeek)))
        {
            schedule.IsActive = true;
            schedule.IsDeleted = false;
            schedule.DeletedAt = null;
            requestedDays.Remove(schedule.DayOfWeek);
        }
        await context.SecretaryAccessSchedules.AddRangeAsync(requestedDays.Select(day => new SecretaryAccessSchedule
            { Id = Guid.NewGuid(), UserId = userId, DayOfWeek = day, IsActive = true }), cancellationToken);
        user.SecretaryType = secretaryType;
        user.UpdatedAt = DateTime.UtcNow;
        await context.SecretaryAccessScheduleAudits.AddAsync(new SecretaryAccessScheduleAudit
        {
            Id = Guid.NewGuid(), SecretaryUserId = userId, ChangedByUserId = changedByUserId,
            OldDays = string.Join(',', oldDays), NewDays = string.Join(',', days.OrderBy(x => x))
        }, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return new(true);
    }
}
