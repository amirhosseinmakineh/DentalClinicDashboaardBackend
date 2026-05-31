using DentalDashboard.Domain.Models;
using DentalDashboard.Framwork.IRepositories;

namespace DentalDashboard.Domain.IRepositories
{
    public interface IUserRoleRepository : IBaseRepository<long, UserRole>
    {
    }
}
