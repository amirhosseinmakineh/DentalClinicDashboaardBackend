using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Models;
using DentalDashboard.Infrastracture.Context;

namespace DentalDashboard.Infrastracture.Repository
{
    public class RoleRepository : BaseRepository<long, Role> , IRoleRepository
    {
        public RoleRepository(DentalContext context) : base(context)
        {
        }
    }

}
