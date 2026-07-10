using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Models;
using DentalDashboard.Infrastracture.Context;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Infrastracture.Repository
{
    public class LeadAssignmentRepository : BaseRepository<long, LeadAssignment>, ILeadAssignmentRepository
    {
        public LeadAssignmentRepository(DentalContext context) : base(context)
        {
        }

        public Task<List<LeadAssignment>> GetUnassignedRealTimeLeadsAsync(int take)
        {
            return GetAll()
                .Where(x => !x.IsDeleted &&
                            x.AssignmentType == LeadAssignmentType.RealTime &&
                            x.ConsultantProfileId == null &&
                            x.ReportSubmittedAt == null &&
                            x.LeadAssignmentState == LeadAssignmentState.New &&
                            x.PickUp == false)
                .OrderBy(x => x.CreatedAt)
                .ThenBy(x => x.Id)
                .Take(take)
                .ToListAsync();
        }

        public Task<List<LeadAssignment>> GetRealtimeLeadsForDispatchAsync(
            int take,
            TimeSpan redispatchInterval)
        {
            var redispatchBefore = DateTime.UtcNow.Subtract(redispatchInterval);

            return GetAll()
                .Where(x => !x.IsDeleted &&
                            x.AssignmentType == LeadAssignmentType.RealTime &&
                            x.ConsultantProfileId == null &&
                            x.ReportSubmittedAt == null &&
                            x.LeadAssignmentState == LeadAssignmentState.New &&
                            !x.PickUp &&
                            (!x.NotificationSent ||
                             x.LastDispatchAt == null ||
                             x.LastDispatchAt < redispatchBefore))
                .OrderBy(x => x.CreatedAt)
                .ThenBy(x => x.Id)
                .Take(take)
                .ToListAsync();
        }

        public async Task<LeadAssignment?> GetActiveRealtimeBroadcastLeadAsync()
        {
            var baseQuery = GetAll()
                .Where(x => !x.IsDeleted &&
                            x.AssignmentType == LeadAssignmentType.RealTime &&
                            x.ConsultantProfileId == null &&
                            x.ReportSubmittedAt == null &&
                            x.LeadAssignmentState == LeadAssignmentState.New &&
                            !x.PickUp);

            var inFlightLead = await baseQuery
                .Where(x => x.NotificationSent)
                .OrderBy(x => x.LastDispatchAt ?? x.CreatedAt)
                .ThenBy(x => x.Id)
                .FirstOrDefaultAsync();

            if (inFlightLead != null)
                return inFlightLead;

            return await baseQuery
                .Where(x => !x.NotificationSent)
                .OrderBy(x => x.CreatedAt)
                .ThenBy(x => x.Id)
                .FirstOrDefaultAsync();
        }

        public async Task<LeadAssignment?> GetCurrentRealtimeLeadForDispatchAsync(
            TimeSpan redispatchInterval)
        {
            var lead = await GetActiveRealtimeBroadcastLeadAsync();
            if (lead == null)
                return null;

            if (!lead.NotificationSent)
                return lead;

            var redispatchBefore = DateTime.UtcNow.Subtract(redispatchInterval);
            if (lead.LastDispatchAt == null || lead.LastDispatchAt < redispatchBefore)
                return lead;

            return null;
        }

        public Task<bool> HasActiveRealTimeLeadAsync(long consultantProfileId)
        {
            return GetAll()
                .AnyAsync(x => !x.IsDeleted &&
                               x.ConsultantProfileId == consultantProfileId &&
                               x.AssignmentType == LeadAssignmentType.RealTime &&
                               x.ReportSubmittedAt == null &&
                               x.LeadAssignmentState == LeadAssignmentState.Assigned);
        }

        public Task<List<LeadAssignment>> GetExpiredRealTimeLeadsAsync(DateTime now)
        {
            return GetAll()
                .Include(x => x.ConsultantProfile!)
                .Where(x => !x.IsDeleted &&
                            x.AssignmentType == LeadAssignmentType.RealTime &&
                            x.RequiresThreeMinuteCall &&
                            x.LeadAssignmentState == LeadAssignmentState.Assigned &&
                            x.ReportSubmittedAt == null &&
                            x.CallInitiatedAt == null &&
                            x.CallDeadlineAt != null &&
                            x.CallDeadlineAt < now)
                .ToListAsync();
        }

        public Task<int> CountUnassignedRealTimeLeadsAsync()
        {
            return GetAll()
                .CountAsync(x => !x.IsDeleted &&
                                 x.AssignmentType == LeadAssignmentType.RealTime &&
                                 x.ConsultantProfileId == null &&
                                 x.ReportSubmittedAt == null &&
                                 x.LeadAssignmentState == LeadAssignmentState.New);
        }

        public async Task<HashSet<string>> GetExistingPhoneNumbersAsync(IEnumerable<string> phoneNumbers)
        {
            var phones = phoneNumbers.Where(x => !string.IsNullOrWhiteSpace(x)).ToHashSet();
            if (!phones.Any())
                return new HashSet<string>();

            return (await GetAll()
                    .Where(x => phones.Contains(x.PhoneNumber))
                    .Select(x => x.PhoneNumber)
                    .ToListAsync())
                .ToHashSet();
        }

        public Task<LeadAssignment?> GetByIdAndConsultantAsync(long leadAssignmentId, long consultantProfileId)
        {
            return GetAll()
                .FirstOrDefaultAsync(x => !x.IsDeleted && x.Id == leadAssignmentId && x.ConsultantProfileId == consultantProfileId);
        }

        public Task<List<LeadAssignment>> GetAssignedLeadsPendingNotificationAsync()
        {
            return GetAll()
                .Include(x => x.ConsultantProfile)
                .ThenInclude(x => x.User)
                .Where(x => !x.IsDeleted &&
                            x.ConsultantProfileId.HasValue &&
                            !x.NotificationSent &&
                            x.LeadAssignmentState == LeadAssignmentState.Assigned)
                .OrderBy(x => x.AssignedAt)
                .ThenBy(x => x.Id)
                .ToListAsync();
        }

        public async Task<int> GetTodayPickupCountAsync(long consultantProfileId)
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            return await context.LeadAssignments
                .CountAsync(x =>
                    !x.IsDeleted &&
                    x.ConsultantProfileId == consultantProfileId &&
                    x.PickUp &&
                    x.AssignedAt >= today &&
                    x.AssignedAt < tomorrow);
        }

        public async Task<bool> TryPickupLeadAsync(
            long leadAssignmentId,
            long consultantProfileId,
            CancellationToken cancellationToken)
        {
            var sql = @"
        UPDATE LeadAssignments
        SET
            ConsultantProfileId = @consultantProfileId,
            PickUp = 1,
            AssignedAt = GETUTCDATE(),
            CallDeadlineAt = DATEADD(MINUTE, 3, GETUTCDATE()),
            LeadAssignmentState = @assignedState
        WHERE Id = @leadAssignmentId
        AND ConsultantProfileId IS NULL
        AND PickUp = 0
    ";

            var affectedRows = await context.Database
                .ExecuteSqlRawAsync(
                    sql,
                    new SqlParameter(
                        "@consultantProfileId",
                        consultantProfileId),
                    new SqlParameter(
                        "@leadAssignmentId",
                        leadAssignmentId),
                    new SqlParameter(
                        "@assignedState",
                        (int)LeadAssignmentState.Assigned)
                );

            return affectedRows == 1;
        }
    }
}
