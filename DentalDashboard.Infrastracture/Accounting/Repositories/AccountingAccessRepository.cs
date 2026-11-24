using System.Data;
using DentalDashboard.Domain.Accounting;
using DentalDashboard.Infrastracture.Context;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Infrastracture.Accounting.Repositories;

public sealed class AccountingAccessRepository(DentalContext db) : IAccountingAccessRepository
{
    public async Task<AccountingAccess?> GetAsync(Guid userId, CancellationToken ct)
    {
        var roles = await db.UserRoles.AsNoTracking().Where(x => x.UserId == userId && !x.IsDeleted && !x.Role.IsDeleted &&
            !x.User.IsDeleted && x.User.IsActive).Select(x => x.Role.RoleName).ToListAsync(ct);
        if (roles.Contains("Admin")) return new(true, AccountingPermissions.All);
        if (!roles.Contains("Accountant")) return null;
        var permissions = await db.Set<AccountantPermission>().AsNoTracking().Where(x => x.UserId == userId)
            .Select(x => x.Permission).ToListAsync(ct);
        return new(false, permissions.Append(AccountingPermissions.View).Where(AccountingPermissions.All.Contains).Distinct().ToArray());
    }
    public async Task<IReadOnlyList<AccountantUser>> UsersAsync(CancellationToken ct)
    {
        var users = await db.Users.AsNoTracking().Where(x => x.IsActive && !x.IsDeleted &&
            x.UserRoles.Any(r => !r.IsDeleted && !r.Role.IsDeleted && r.Role.RoleName == "Accountant"))
            .OrderBy(x => x.LastName).Select(x => new { x.Id, Name = x.FirstName + " " + x.LastName, x.PhoneNumber }).ToListAsync(ct);
        var grants = await db.Set<AccountantPermission>().AsNoTracking().ToListAsync(ct);
        return users.Select(x => new AccountantUser(x.Id, x.Name, x.PhoneNumber,
            grants.Where(g => g.UserId == x.Id).Select(g => g.Permission).Append(AccountingPermissions.View).Distinct().ToArray())).ToArray();
    }
    public async Task<bool> ReplaceAsync(Guid userId, Guid actor, IReadOnlyList<string> permissions, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        if (!await db.Users.AnyAsync(x => x.Id == userId && x.IsActive && !x.IsDeleted && x.UserRoles.Any(r =>
            !r.IsDeleted && !r.Role.IsDeleted && r.Role.RoleName == "Accountant"), ct)) return false;
        var current = await db.Set<AccountantPermission>().Where(x => x.UserId == userId).ToListAsync(ct);
        db.RemoveRange(current.Where(x => !permissions.Contains(x.Permission)));
        foreach (var permission in permissions.Where(p => p != AccountingPermissions.View && !current.Any(x => x.Permission == p)))
            db.Add(new AccountantPermission { UserId = userId, Permission = permission, GrantedByUserId = actor });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return true;
    }
}
