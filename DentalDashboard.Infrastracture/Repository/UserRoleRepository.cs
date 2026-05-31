using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Models;
using DentalDashboard.Infrastracture.Context;

namespace DentalDashboard.Infrastracture.Repository
{
    public class UserRoleRepository : BaseRepository<long, UserRole> , IUserRoleRepository
    {
        public UserRoleRepository(DentalContext context) : base(context)
        {
        }
    }

}
