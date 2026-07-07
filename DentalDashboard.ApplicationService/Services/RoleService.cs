using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Models;

namespace DentalDashboard.ApplicationService.Services;

public class RoleService : IRoleService
{
    private readonly IRoleRepository roleRepository;
    private readonly IUserRoleRepository userRoleRepository;

    public RoleService(IRoleRepository roleRepository, IUserRoleRepository userRoleRepository)
    {
        this.roleRepository = roleRepository;
        this.userRoleRepository = userRoleRepository;
    }

    public async Task AddRoleToUser(Guid userId, string roleName)
    {
        var roles = await roleRepository.GetAllAsync();

        var role = roles.FirstOrDefault(
            x => x.RoleName == roleName);

        if (role is null)
        {
            throw new Exception(
                $"Role '{roleName}' یافت نشد");
        }

        var userRoles = (await userRoleRepository.FindAsync(x => x.UserId == userId)).ToList();

        var activeUserRole = userRoles.FirstOrDefault(
            x => x.RoleId == role.Id && !x.IsDeleted);

        if (activeUserRole is not null)
            return;

        var inactiveUserRole = userRoles
            .Where(x => x.RoleId == role.Id && x.IsDeleted)
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .FirstOrDefault();

        if (inactiveUserRole is not null)
        {
            var now = DateTime.UtcNow;
            inactiveUserRole.IsDeleted = false;
            inactiveUserRole.DeletedAt = null;
            inactiveUserRole.UpdatedAt = now;
            userRoleRepository.Update(inactiveUserRole);
            await userRoleRepository.SaveChange();
            return;
        }

        await userRoleRepository.AddAsync(new UserRole
        {
            UserId = userId,
            RoleId = role.Id
        });

        await userRoleRepository.SaveChange();
    }

    public async Task SetUserRole(Guid userId, string roleName)
    {
        var roles = await roleRepository.GetAllAsync();
        var role = roles.FirstOrDefault(x => x.RoleName == roleName);

        if (role is null)
            throw new Exception($"Role '{roleName}' یافت نشد");

        var userRoles = (await userRoleRepository.FindAsync(x => x.UserId == userId)).ToList();
        var now = DateTime.UtcNow;

        foreach (var userRole in userRoles.Where(x => !x.IsDeleted))
        {
            userRole.IsDeleted = true;
            userRole.DeletedAt = now;
            userRole.UpdatedAt = now;
            userRoleRepository.Update(userRole);
        }

        var existingRoleRow = userRoles
            .Where(x => x.RoleId == role.Id)
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .FirstOrDefault();

        if (existingRoleRow is not null)
        {
            existingRoleRow.IsDeleted = false;
            existingRoleRow.DeletedAt = null;
            existingRoleRow.UpdatedAt = now;
            userRoleRepository.Update(existingRoleRow);
        }
        else
        {
            await userRoleRepository.AddAsync(new UserRole
            {
                UserId = userId,
                RoleId = role.Id
            });
        }

        await userRoleRepository.SaveChange();
    }
}
