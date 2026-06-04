using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Models;
using DentalDashboard.Infrastracture.Context;

namespace DentalDashboard.Infrastracture.Repository
{
    public class LeadAssignmentRepository : BaseRepository<long, LeadAssignment> , ILeadAssignmentRepository
    {
        public LeadAssignmentRepository(DentalContext context) : base(context)
        {
        }

        public async Task<List<LeadAssignment>> GetPendingOfflineQueueAsync()
        {
            return  GetAll()
                .Where(x =>
                 x.AssignmentType == LeadAssignmentType.OfflineQueue &&
                 x.LeadAssignmentState == LeadAssignmentState.Pending)
                    .ToList();
        }

        public async Task<bool> HasPendingOfflineLeadsAsync(long consultantProfileId)
        {
            {
                return  GetAll()
                    .Any(x =>
                        x.ConsultantProfileId == consultantProfileId &&
                        x.AssignmentType == LeadAssignmentType.OfflineQueue &&
                        x.LeadAssignmentState == LeadAssignmentState.Assigned);
            }
        }
    }

}
