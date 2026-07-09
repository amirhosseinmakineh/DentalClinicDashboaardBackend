using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Models;
using DentalDashboard.Infrastracture.Context;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Infrastracture.Repository
{
    public class ConsultantProfileRepository : BaseRepository<long, ConsultantProfile>, IConsultantProfileRepository
    {
        public ConsultantProfileRepository(DentalContext context) : base(context)
        {
        }

        public Task<List<ConsultantProfile>> GetAvailableConsultantsAsync()
        {
            return GetAll()
                .Where(x => !x.IsDeleted &&
                            x.IsCompleteProfile &&
                            x.IsAvailable &&
                            !x.IsOnline)
                .OrderBy(x => x.Id)
                .ToListAsync();
        }

        public Task<List<ConsultantProfile>> GetOnlineConsultantsReadyForRealTimeAsync()
        {
            return GetAll()
                .Where(x => !x.IsDeleted &&
                            x.IsCompleteProfile &&
                            x.IsAvailable &&
                            x.IsOnline &&
                            !x.CallAssignments.Any(l => l.AssignmentType == LeadAssignmentType.RealTime &&
                                                        l.LeadAssignmentState == LeadAssignmentState.Assigned &&
                                                        l.ReportSubmittedAt == null))
                .OrderBy(x => x.Id)
                .ToListAsync();
        }

        public Task<bool> HasOnlineConsultantAsync()
        {
            return GetAll()
                .AnyAsync(x => !x.IsDeleted &&
                               x.IsCompleteProfile &&
                               x.IsAvailable &&
                               x.IsOnline);
        }
    }
}
