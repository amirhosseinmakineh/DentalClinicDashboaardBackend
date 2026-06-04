using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Models;
using DentalDashboard.Infrastracture.Context;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Infrastracture.Repository
{
    public class ConsultantProfileRepository : BaseRepository<long, ConsultantProfile> ,IConsultantProfileRepository
    {
        public ConsultantProfileRepository(DentalContext context) : base(context)
        {
        }

        public  Task<List<ConsultantProfile>> GetAvailableConsultantsAsync()
        {
            return  GetAll()
                .Where(x =>
                    !x.IsDeleted &&
                    x.IsCompleteProfile &&
                    x.IsAvailable)
                    .Include(x => x.ScoreLogs)
                    .ToListAsync();
        }
    }

}
