using DentalDashboard.Domain.Models;
using DentalDashboard.Framwork.IRepositories;

namespace DentalDashboard.Infrastracture.Repository
{
    public interface IServiceLogRepository:IBaseRepository<long,ServiceLog>
    {
    }
}
