namespace DentalDashboard.ApplicationService.Contract.IServices
{
    public interface IRoleService
    {
        Task AddRoleToUser(Guid userId, string roleName);

        Task SetUserRole(Guid userId, string roleName);
    }
}
