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

        public Task<List<Attendance>> GetConsultantAttendance(long profileId)
        {
            return GetAll()
                .SelectMany(x=> x.Attendances)
                .Where(x=> x.ConsultantProfileId == profileId)
                .ToListAsync();
        }

        public Task<List<ScoreLog>> GetConsultantScoreLog(long profileId)
        {
            return GetAll()
                .SelectMany (x=> x.ScoreLogs)
                .Where(x=> x.ConsultantProfileId == profileId)
                .ToListAsync();
        }
    }

}
