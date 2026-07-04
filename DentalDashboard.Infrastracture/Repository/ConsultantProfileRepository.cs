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
            return GetAvailableConsultantsForOfflineAssignmentAsync();
        }

        public Task<List<ConsultantProfile>> GetAvailableConsultantsForOfflineAssignmentAsync()
        {
            return GetAll()
                .Where(x => !x.IsDeleted && x.IsCompleteProfile && x.IsAvailable)
                .OrderByDescending(x => x.CurrentScore)
                .ThenBy(x => x.Id)
                .ToListAsync();
        }

        public Task<List<ConsultantProfile>> GetOnlineConsultantsReadyForRealTimeAsync()
        {
            return GetAll()
                .Where(x => !x.IsDeleted &&
                            x.IsCompleteProfile &&
                            x.IsAvailable &&
                            x.IsOnline &&
                            !x.CallAssignments.Any(l => l.AssignmentType == LeadAssignmentType.OfflineQueue &&
                                                        l.ReportSubmittedAt == null &&
                                                        l.LeadAssignmentState != LeadAssignmentState.Converted &&
                                                        l.LeadAssignmentState != LeadAssignmentState.Rejected &&
                                                        l.LeadAssignmentState != LeadAssignmentState.Expired) &&
                            !x.CallAssignments.Any(l => l.AssignmentType == LeadAssignmentType.RealTime &&
                                                        (l.LeadAssignmentState == LeadAssignmentState.Assigned ||
                                                         l.LeadAssignmentState == LeadAssignmentState.Claimed) &&
                                                        l.ReportSubmittedAt == null))
                .OrderByDescending(x => x.CurrentScore)
                .ThenBy(x => x.Id)
                .ToListAsync();
        }
    }
}
