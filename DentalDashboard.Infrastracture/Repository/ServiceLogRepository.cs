using DentalDashboard.Domain.Models;
using DentalDashboard.Infrastracture.Context;

namespace DentalDashboard.Infrastracture.Repository
{
    public class ServiceLogRepository : BaseRepository<long, ServiceLog>,IServiceLogRepository
    {
        public ServiceLogRepository(DentalContext context) : base(context)
        {
        }
    }
}
