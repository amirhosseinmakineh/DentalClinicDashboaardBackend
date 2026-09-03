using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Models;
using DentalDashboard.Infrastracture.Context;
using DentalDashboard.Utilities.Time;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

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
                .OrderByDescending(x => x.CreatedAt)
                .ThenBy(x => x.Id)
                .Take(take)
                .ToListAsync();
        }

        private IQueryable<LeadAssignment> AssignmentCandidates(LeadAssignmentSourceType sourceType)
        {
            var allLeads = GetAll();
            if (sourceType == LeadAssignmentSourceType.BurnedLeads)
            {
                return allLeads.Where(x =>
                    (x.IsDeleted && x.ConsultantProfileId == null) ||
                    (!x.IsDeleted && x.ConsultantProfileId != null &&
                     x.LeadAssignmentState == LeadAssignmentState.Pending));
            }

            var newLeads = allLeads.Where(x => !x.IsDeleted &&
                    x.AssignmentType == LeadAssignmentType.RealTime &&
                    x.ConsultantProfileId == null &&
                    x.ReportSubmittedAt == null &&
                    x.LeadAssignmentState == LeadAssignmentState.New &&
                    !x.PickUp);

            var previousUnassignedLeads = allLeads.Where(x =>
                x.IsDeleted &&
                x.ConsultantProfileId == null &&
                !newLeads.Any());

            return newLeads.Concat(previousUnassignedLeads);
        }

        public async Task<LeadAssignment?> GetActiveRealtimeBroadcastLeadAsync(LeadAssignmentSourceType sourceType)
        {
            var baseQuery = AssignmentCandidates(sourceType);

            var inFlightLead = await baseQuery
                .Where(x => x.NotificationSent)
                .OrderByDescending(x =>  x.CreatedAt)
                .ThenBy(x => x.Id)
                .FirstOrDefaultAsync();

            if (inFlightLead != null)
                return inFlightLead;

            return await baseQuery
                .Where(x => !x.NotificationSent)
                .OrderByDescending(x => x.CreatedAt)
                .ThenBy(x => x.Id)
                .FirstOrDefaultAsync();
        }

        public async Task<LeadAssignment?> GetCurrentRealtimeLeadForDispatchAsync(
            LeadAssignmentSourceType sourceType,
            TimeSpan redispatchInterval)
        {
            var lead = await GetActiveRealtimeBroadcastLeadAsync(sourceType);
            if (lead == null)
                return null;

            if (!lead.NotificationSent)
                return lead;

            var redispatchBefore = DateTime.UtcNow.Subtract(redispatchInterval);
            if (lead.LastDispatchAt == null || lead.LastDispatchAt < redispatchBefore)
                return lead;

            return null;
        }

        public Task<int> CountAssignmentCandidatesAsync(
            LeadAssignmentSourceType sourceType,
            CancellationToken cancellationToken = default) =>
            AssignmentCandidates(sourceType).CountAsync(cancellationToken);

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

        public async Task<HashSet<string>> GetExistingPhoneNumbersAsync(
            IEnumerable<string> phoneNumbers,
            CancellationToken cancellationToken = default)
        {
            const int batchSize = 500;
            var phones = phoneNumbers
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToArray();
            var existingPhones = new HashSet<string>();

            foreach (var batch in phones.Chunk(batchSize))
            {
                var matches = await GetAll()
                    .AsNoTracking()
                    .Where(x => batch.Contains(x.PhoneNumber))
                    .Select(x => x.PhoneNumber)
                    .ToListAsync(cancellationToken);

                existingPhones.UnionWith(matches);
            }

            return existingPhones;
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
            var today = IranTimeHelper.TodayInIran();
            var (todayStartUtc, _) = IranTimeHelper.GetIranDayRangeAsUtc(today);
            var (tomorrowStartUtc, _) = IranTimeHelper.GetIranDayRangeAsUtc(today.AddDays(1));

            return await context.LeadAssignments
                .CountAsync(x =>
                    !x.IsDeleted &&
                    x.ConsultantProfileId == consultantProfileId &&
                    x.PickUp &&
                    x.AssignedAt >= todayStartUtc &&
                    x.AssignedAt < tomorrowStartUtc);
        }

        public async Task<int> GetTodayCallCountAsync(long consultantProfileId)
        {
            var today = IranTimeHelper.TodayInIran();
            var (todayStartUtc, _) = IranTimeHelper.GetIranDayRangeAsUtc(today);
            var (tomorrowStartUtc, _) = IranTimeHelper.GetIranDayRangeAsUtc(today.AddDays(1));

            return await context.LeadAssignments
                .AsNoTracking()
                .CountAsync(x =>
                    !x.IsDeleted &&
                    x.ConsultantProfileId == consultantProfileId &&
                    x.CallInitiatedAt >= todayStartUtc &&
                    x.CallInitiatedAt < tomorrowStartUtc);
        }

        public async Task<bool> TryPickupLeadAsync(
            long leadAssignmentId,
            long consultantProfileId,
            CancellationToken cancellationToken)
        {
            await using var transaction = await context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

            var sourceType = await context.LeadAssignmentSettings
                .AsNoTracking()
                .Where(x => x.Id == LeadAssignmentSetting.SingletonId)
                .Select(x => x.AssignmentSourceType)
                .SingleOrDefaultAsync(cancellationToken);
            if (!Enum.IsDefined(sourceType))
                sourceType = LeadAssignmentSourceType.NewLeads;

            var lead = await context.LeadAssignments
                .FromSqlInterpolated($"SELECT * FROM LeadAssignments WITH (UPDLOCK, ROWLOCK) WHERE Id = {leadAssignmentId}")
                .AsNoTracking()
                .SingleOrDefaultAsync(cancellationToken);
            var hasNewLead = sourceType == LeadAssignmentSourceType.NewLeads &&
                await context.LeadAssignments.AnyAsync(x =>
                    !x.IsDeleted &&
                    x.AssignmentType == LeadAssignmentType.RealTime &&
                    x.ConsultantProfileId == null &&
                    x.ReportSubmittedAt == null &&
                    x.LeadAssignmentState == LeadAssignmentState.New &&
                    !x.PickUp,
                    cancellationToken);
            if (lead == null || !IsEligibleForPickup(lead, sourceType, hasNewLead))
                return false;

            const string sql = @"
UPDATE LeadAssignments WITH (UPDLOCK, ROWLOCK)
SET ConsultantProfileId = @consultantProfileId,
    IsDeleted = 0,
    DeletedAt = NULL,
    PickUp = 1,
    AssignedAt = GETUTCDATE(),
    CallDeadlineAt = DATEADD(MINUTE, 3, GETUTCDATE()),
    CallInitiatedAt = NULL,
    LeadAssignmentState = @assignedState,
    AssignmentType = @realTimeType,
    RequiresThreeMinuteCall = 1,
    ReportDescription = CASE WHEN @sourceType = @burnedSource OR IsDeleted = 1 THEN NULL ELSE ReportDescription END,
    ReportSubmittedAt = CASE WHEN @sourceType = @burnedSource OR IsDeleted = 1 THEN NULL ELSE ReportSubmittedAt END,
    ContactedAt = CASE WHEN @sourceType = @burnedSource OR IsDeleted = 1 THEN NULL ELSE ContactedAt END,
    CallResult = CASE WHEN @sourceType = @burnedSource OR IsDeleted = 1 THEN NULL ELSE CallResult END,
    UpdatedAt = GETUTCDATE()
WHERE Id = @leadAssignmentId
  AND (
      (@sourceType = @newSource
       AND (
           (IsDeleted = 0
            AND AssignmentType = @realTimeType
            AND ConsultantProfileId IS NULL
            AND ReportSubmittedAt IS NULL
            AND LeadAssignmentState = @newState
            AND PickUp = 0)
           OR
           (IsDeleted = 1
            AND ConsultantProfileId IS NULL
            AND NOT EXISTS (
                SELECT 1
                FROM LeadAssignments AS NewLead WITH (UPDLOCK, HOLDLOCK)
                WHERE NewLead.IsDeleted = 0
                  AND NewLead.AssignmentType = @realTimeType
                  AND NewLead.ConsultantProfileId IS NULL
                  AND NewLead.ReportSubmittedAt IS NULL
                  AND NewLead.LeadAssignmentState = @newState
                  AND NewLead.PickUp = 0)))
       )
      )
      OR
      (@sourceType = @burnedSource
       AND ((IsDeleted = 1 AND ConsultantProfileId IS NULL)
            OR (IsDeleted = 0 AND ConsultantProfileId IS NOT NULL
                AND LeadAssignmentState = @pendingState)))
  );";

            var affectedRows = await context.Database.ExecuteSqlRawAsync(
                sql,
                new SqlParameter("@consultantProfileId", consultantProfileId),
                new SqlParameter("@leadAssignmentId", leadAssignmentId),
                new SqlParameter("@assignedState", (int)LeadAssignmentState.Assigned),
                new SqlParameter("@newState", (int)LeadAssignmentState.New),
                new SqlParameter("@pendingState", (int)LeadAssignmentState.Pending),
                new SqlParameter("@realTimeType", (int)LeadAssignmentType.RealTime),
                new SqlParameter("@sourceType", (int)sourceType),
                new SqlParameter("@newSource", (int)LeadAssignmentSourceType.NewLeads),
                new SqlParameter("@burnedSource", (int)LeadAssignmentSourceType.BurnedLeads));

            if (affectedRows != 1)
                return false;

            await context.LeadAssignmentHistories.AddAsync(new LeadAssignmentHistory
            {
                LeadAssignmentId = lead.Id,
                PreviousConsultantProfileId = lead.ConsultantProfileId,
                NewConsultantProfileId = consultantProfileId,
                AssignmentSourceType = sourceType,
                PreviousState = lead.LeadAssignmentState,
                PreviousAssignedAt = lead.AssignedAt,
                PreviousReportDescription = lead.ReportDescription,
                PreviousReportSubmittedAt = lead.ReportSubmittedAt,
                PreviousCallResult = lead.CallResult,
                AssignedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return true;
        }

        private static bool IsEligibleForPickup(
            LeadAssignment lead,
            LeadAssignmentSourceType sourceType,
            bool hasNewLead) =>
            sourceType == LeadAssignmentSourceType.BurnedLeads
                ? (lead.IsDeleted && lead.ConsultantProfileId == null) ||
                  (!lead.IsDeleted && lead.ConsultantProfileId != null &&
                   lead.LeadAssignmentState == LeadAssignmentState.Pending)
                : (!lead.IsDeleted &&
                   lead.AssignmentType == LeadAssignmentType.RealTime &&
                   lead.ConsultantProfileId == null &&
                   lead.ReportSubmittedAt == null &&
                   lead.LeadAssignmentState == LeadAssignmentState.New &&
                   !lead.PickUp) ||
                  (!hasNewLead && lead.IsDeleted && lead.ConsultantProfileId == null);
    }
}
