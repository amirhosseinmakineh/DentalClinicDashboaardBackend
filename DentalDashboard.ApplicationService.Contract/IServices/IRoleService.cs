using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DentalDashboard.ApplicationService.Contract.IServices
{
    public interface IRoleService
    {
          Task AddRoleToUser(Guid userId, string roleName);
    }
}
