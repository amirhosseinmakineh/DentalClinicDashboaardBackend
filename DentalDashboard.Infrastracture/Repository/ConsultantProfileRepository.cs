using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Models;
using DentalDashboard.Infrastracture.Context;

namespace DentalDashboard.Infrastracture.Repository
{
    public class ConsultantProfileRepository : BaseRepository<long, ConsultantProfile> ,IConsultantProfileRepository
    {
        public ConsultantProfileRepository(DentalContext context) : base(context)
        {
        }
    }

}
