using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Models;
using DentalDashboard.Infrastracture.Context;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Infrastracture.Repository
{
    public class LeadAssignmentRepository : BaseRepository<long, LeadAssignment>, ILeadAssignmentRepository
    {
        public LeadAssignmentRepository(DentalContext context) : base(context)
        {
        }

        public Task<List<LeadAssignment>> GetPendingOfflineQueueAsync()
        {
            return GetPendingOfflineLeadsAsync(100);
        }

        public Task<List<LeadAssignment>> GetPendingOfflineLeadsAsync(int take)
        {
            return GetAll()
                .Where(x => x.AssignmentType == LeadAssignmentType.OfflineQueue &&
                            x.LeadAssignmentState == LeadAssignmentState.Pending &&
                            x.ConsultantProfileId == null)
                .OrderBy(x => x.CreatedAt)
                .ThenBy(x => x.Id)
                .Take(take)
                .ToListAsync();
        }

        public Task<List<LeadAssignment>> GetUnassignedRealTimeLeadsAsync(int take)
        {
            return GetAll()
                .Where(x => x.AssignmentType == LeadAssignmentType.RealTime &&
                            x.LeadAssignmentState == LeadAssignmentState.New &&
                            x.ConsultantProfileId == null)
                .OrderBy(x => x.CreatedAt)
                .ThenBy(x => x.Id)
                .Take(take)
                .ToListAsync();
        }

        public Task<bool> HasPendingOfflineLeadsAsync(long consultantProfileId)
        {
            return GetAll()
                .AnyAsync(x => x.ConsultantProfileId == consultantProfileId &&
                               x.AssignmentType == LeadAssignmentType.OfflineQueue &&
                               x.ReportSubmittedAt == null &&
                               x.LeadAssignmentState != LeadAssignmentState.Converted &&
                               x.LeadAssignmentState != LeadAssignmentState.Rejected &&
                               x.LeadAssignmentState != LeadAssignmentState.Expired);
        }

        public Task<List<LeadAssignment>> GetExpiredRealTimeLeadsAsync(DateTime now)
        {
            return GetAll()
                .Include(x => x.ConsultantProfile)
                .Where(x => x.AssignmentType == LeadAssignmentType.RealTime &&
                            x.RequiresThreeMinuteCall &&
                            x.LeadAssignmentState == LeadAssignmentState.Assigned &&
                            x.ReportSubmittedAt == null &&
                            x.CallDeadlineAt != null &&
                            x.CallDeadlineAt < now)
                .ToListAsync();
        }

        public async Task<HashSet<string>> GetExistingPhoneNumbersAsync(IEnumerable<string> phoneNumbers)
        {
            var phones = phoneNumbers.Where(x => !string.IsNullOrWhiteSpace(x)).ToHashSet();
            return (await GetAll()
                    .Where(x => phones.Contains(x.PhoneNumber))
                    .Select(x => x.PhoneNumber)
                    .ToListAsync())
                .ToHashSet();
        }

        public Task<LeadAssignment?> GetByIdAndConsultantAsync(long leadAssignmentId, long consultantProfileId)
        {
            return GetAll()
                .FirstOrDefaultAsync(x => x.Id == leadAssignmentId && x.ConsultantProfileId == consultantProfileId);
        }
    }
}
