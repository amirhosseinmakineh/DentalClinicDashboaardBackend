using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Models;
using DentalDashboard.Infrastracture.Context;

namespace DentalDashboard.Infrastracture.Repository;

public class UserPresenceLogRepository : BaseRepository<long, UserPresenceLog>, IUserPresenceLogRepository
{
    public UserPresenceLogRepository(DentalContext context) : base(context)
    {
    }
}
