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
                .Where(x => !x.IsDeleted &&
                            x.AssignmentType == LeadAssignmentType.OfflineQueue &&
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
                .Where(x => !x.IsDeleted &&
                            x.AssignmentType == LeadAssignmentType.RealTime &&
                            x.LeadAssignmentState == LeadAssignmentState.New &&
                            x.ConsultantProfileId == null)
                .OrderBy(x => x.CreatedAt)
                .ThenBy(x => x.Id)
                .Take(take)
                .ToListAsync();
        }

        public Task<bool> HasPendingOfflineLeadsAsync(long consultantProfileId)
        {
            return PendingOfflineLeadsForConsultant(consultantProfileId).AnyAsync();
        }

        public Task<int> CountPendingOfflineLeadsAsync(long consultantProfileId)
        {
            return PendingOfflineLeadsForConsultant(consultantProfileId).CountAsync();
        }

        private IQueryable<LeadAssignment> PendingOfflineLeadsForConsultant(long consultantProfileId)
        {
            return GetAll()
                .Where(x => !x.IsDeleted &&
                            x.ConsultantProfileId == consultantProfileId &&
                            x.AssignmentType == LeadAssignmentType.OfflineQueue &&
                            x.ReportSubmittedAt == null &&
                            x.LeadAssignmentState != LeadAssignmentState.Converted &&
                            x.LeadAssignmentState != LeadAssignmentState.Rejected &&
                            x.LeadAssignmentState != LeadAssignmentState.Expired);
        }

        public Task<bool> HasActiveRealTimeLeadAsync(long consultantProfileId)
        {
            return GetAll()
                .AnyAsync(x => !x.IsDeleted &&
                               x.ConsultantProfileId == consultantProfileId &&
                               x.AssignmentType == LeadAssignmentType.RealTime &&
                               x.ReportSubmittedAt == null &&
                               (x.LeadAssignmentState == LeadAssignmentState.Assigned ||
                                x.LeadAssignmentState == LeadAssignmentState.Claimed));
        }

        public Task<List<LeadAssignment>> GetExpiredRealTimeLeadsAsync(DateTime now)
        {
            return GetAll()
                .Include(x => x.ConsultantProfile)
                .Where(x => !x.IsDeleted &&
                            x.AssignmentType == LeadAssignmentType.RealTime &&
                            x.RequiresThreeMinuteCall &&
                            x.LeadAssignmentState == LeadAssignmentState.Assigned &&
                            x.ReportSubmittedAt == null &&
                            x.CallDeadlineAt != null &&
                            x.CallDeadlineAt < now)
                .ToListAsync();
        }


        public Task<int> CountUnassignedRealTimeLeadsAsync()
        {
            return GetAll()
                .CountAsync(x => !x.IsDeleted &&
                                 x.AssignmentType == LeadAssignmentType.RealTime &&
                                 (x.LeadAssignmentState == LeadAssignmentState.New ||
                                  x.LeadAssignmentState == LeadAssignmentState.Broadcasting) &&
                                 x.ConsultantProfileId == null);
        }

        public async Task<HashSet<long>> GetConsultantIdsWithPendingOfflineLeadsAsync(IEnumerable<long> consultantProfileIds)
        {
            var ids = consultantProfileIds.ToHashSet();
            if (!ids.Any())
                return new HashSet<long>();

            return (await GetAll()
                    .Where(x => !x.IsDeleted &&
                                x.ConsultantProfileId.HasValue &&
                                ids.Contains(x.ConsultantProfileId.Value) &&
                                x.AssignmentType == LeadAssignmentType.OfflineQueue &&
                                x.ReportSubmittedAt == null &&
                                x.LeadAssignmentState != LeadAssignmentState.Converted &&
                                x.LeadAssignmentState != LeadAssignmentState.Rejected &&
                                x.LeadAssignmentState != LeadAssignmentState.Expired)
                    .Select(x => x.ConsultantProfileId!.Value)
                    .Distinct()
                    .ToListAsync())
                .ToHashSet();
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

        public async Task<Dictionary<long, int>> GetDailyAssignedOfflineLeadCountsAsync(IEnumerable<long> consultantProfileIds, DateTime day)
        {
            var ids = consultantProfileIds.ToHashSet();
            if (!ids.Any())
                return new Dictionary<long, int>();

            var startOfDay = day.Date;
            var endOfDay = startOfDay.AddDays(1);

            return await GetAll()
                .Where(x => !x.IsDeleted &&
                                x.ConsultantProfileId.HasValue &&
                            ids.Contains(x.ConsultantProfileId.Value) &&
                            x.AssignmentType == LeadAssignmentType.OfflineQueue &&
                            x.AssignedAt.HasValue &&
                            x.AssignedAt.Value >= startOfDay &&
                            x.AssignedAt.Value < endOfDay)
                .GroupBy(x => x.ConsultantProfileId!.Value)
                .Select(g => new { ConsultantProfileId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ConsultantProfileId, x => x.Count);
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

        public Task<List<LeadAssignment>> GetBroadcastingLeadsAsync(long consultantProfileId, int take = 20)
        {
            var dismissedQuery = context.Set<LeadBroadcastDismissal>()
                .Where(x => !x.IsDeleted && x.ConsultantProfileId == consultantProfileId)
                .Select(x => x.LeadAssignmentId);

            return GetAll()
                .Where(x => !x.IsDeleted &&
                            x.LeadAssignmentState == LeadAssignmentState.Broadcasting &&
                            !dismissedQuery.Contains(x.Id))
                .OrderByDescending(x => x.BroadcastStartedAt ?? x.CreatedAt)
                .Take(take)
                .ToListAsync();
        }

        public Task<List<long>> GetDismissedBroadcastLeadIdsAsync(long consultantProfileId)
        {
            return context.Set<LeadBroadcastDismissal>()
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.ConsultantProfileId == consultantProfileId)
                .Select(x => x.LeadAssignmentId)
                .ToListAsync();
        }

        public Task<bool> IsBroadcastDismissedAsync(long leadAssignmentId, long consultantProfileId)
        {
            return context.Set<LeadBroadcastDismissal>()
                .AnyAsync(x => !x.IsDeleted &&
                               x.LeadAssignmentId == leadAssignmentId &&
                               x.ConsultantProfileId == consultantProfileId);
        }

        public async Task AddBroadcastDismissalAsync(long leadAssignmentId, long consultantProfileId)
        {
            context.Set<LeadBroadcastDismissal>().Add(new LeadBroadcastDismissal
            {
                LeadAssignmentId = leadAssignmentId,
                ConsultantProfileId = consultantProfileId,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            });

            await Task.CompletedTask;
        }

        public Task<List<LeadAssignment>> GetStaleBroadcastingLeadsAsync(DateTime now)
        {
            return GetAll()
                .Where(x => !x.IsDeleted &&
                            x.LeadAssignmentState == LeadAssignmentState.Broadcasting &&
                            x.BroadcastExpiresAt != null &&
                            x.BroadcastExpiresAt <= now)
                .ToListAsync();
        }

        public Task<List<LeadAssignment>> GetPendingBroadcastRealTimeLeadsAsync(int take)
        {
            return GetAll()
                .Where(x => !x.IsDeleted &&
                            x.AssignmentType == LeadAssignmentType.RealTime &&
                            x.LeadAssignmentState == LeadAssignmentState.New &&
                            x.ConsultantProfileId == null)
                .OrderBy(x => x.CreatedAt)
                .ThenBy(x => x.Id)
                .Take(take)
                .ToListAsync();
        }

    }
}
