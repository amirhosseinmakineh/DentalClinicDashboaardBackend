using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Infrastracture.Context;

namespace DentalDashboard.Infrastracture.Repository
{
    public class ScoreLogRepository : BaseRepository<long, ScoreLog>, IScoreLogRepository
    {
        public ScoreLogRepository(DentalContext context) : base(context)
        {
        }
    }

}
