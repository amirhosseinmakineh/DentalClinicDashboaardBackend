using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Models;
using DentalDashboard.Infrastracture.Context;

namespace DentalDashboard.Infrastracture.Repository
{
    public class UserRepository : BaseRepository<Guid,User>,IUserRepository
    {
        public UserRepository(DentalContext context) : base(context)
        {
        }
    }

}
