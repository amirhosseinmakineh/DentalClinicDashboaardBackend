using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Models;
using DentalDashboard.Infrastracture.Context;

namespace DentalDashboard.Infrastracture.Repository
{
    public class AttendanceRepository : BaseRepository<long, Attendance>, IAttendanceRepository
    {
        public AttendanceRepository(DentalContext context) : base(context)
        {
        }
    }

}
