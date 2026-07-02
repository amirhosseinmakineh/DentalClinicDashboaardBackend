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

        var userRoles = await userRoleRepository.GetAllAsync();

        var existingUserRole = userRoles.FirstOrDefault(
            x => x.UserId == userId &&
                 x.RoleId == role.Id &&
                 !x.IsDeleted);

        if (existingUserRole is not null)
        {
            return;
        }

        var userRole = new UserRole
        {
            UserId = userId,
            RoleId = role.Id
        };

        await userRoleRepository.AddAsync(userRole);

        await userRoleRepository.SaveChange();
    }

    public async Task SetUserRole(Guid userId, string roleName)
    {
        var roles = await roleRepository.GetAllAsync();
        var role = roles.FirstOrDefault(x => x.RoleName == roleName);

        if (role is null)
            throw new Exception($"Role '{roleName}' یافت نشد");

        var userRoles = await userRoleRepository.FindAsync(x => x.UserId == userId);
        var now = DateTime.UtcNow;

        foreach (var userRole in userRoles.Where(x => !x.IsDeleted))
        {
            if (userRole.RoleId == role.Id)
                continue;

            userRole.IsDeleted = true;
            userRole.DeletedAt = now;
            userRole.UpdatedAt = now;
            userRoleRepository.Update(userRole);
        }

        var activeRole = userRoles.FirstOrDefault(x => x.RoleId == role.Id && !x.IsDeleted);
        if (activeRole is null)
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
