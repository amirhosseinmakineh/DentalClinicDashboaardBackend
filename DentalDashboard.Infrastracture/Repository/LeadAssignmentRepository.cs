using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Models;
using DentalDashboard.Infrastracture.Context;
using DentalDashboard.Utilities.Time;
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
                .OrderByDescending(x => x.CreatedAt)
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

        private IQueryable<LeadAssignment> BurnedLeads() =>
            context.LeadAssignments.IgnoreQueryFilters().Where(x =>
                x.AssignmentType == LeadAssignmentType.RealTime &&
                ((x.ConsultantProfileId == null && !x.PickUp) ||
                 (x.ConsultantProfileId != null &&
                  x.CallResult == LeadCallResult.NoAnswer &&
                  x.ReportSubmittedAt != null)));

        public async Task<LeadAssignment?> GetActiveBurnedLeadAsync()
        {
            var candidates = BurnedLeads();
            return await candidates.Where(x => x.NotificationSent).OrderBy(x => x.CreatedAt).ThenBy(x => x.Id).FirstOrDefaultAsync()
                ?? await candidates.Where(x => !x.NotificationSent).OrderBy(x => x.CreatedAt).ThenBy(x => x.Id).FirstOrDefaultAsync();
        }

        public async Task<LeadAssignment?> GetCurrentBurnedLeadForDispatchAsync(TimeSpan redispatchInterval)
        {
            var lead = await GetActiveBurnedLeadAsync();
            if (lead == null || !lead.NotificationSent)
                return lead;

            return lead.LastDispatchAt == null || lead.LastDispatchAt < DateTime.UtcNow.Subtract(redispatchInterval)
                ? lead
                : null;
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
            var sourceType = await context.LeadAssignmentSettings.AsNoTracking()
                .Where(x => x.Id == LeadAssignmentSetting.SingletonId)
                .Select(x => x.AssignmentSourceType)
                .SingleOrDefaultAsync(cancellationToken);
            if (!Enum.IsDefined(sourceType))
                sourceType = LeadAssignmentSourceType.NewLeads;

            var sql = @"
        UPDATE LeadAssignments WITH (UPDLOCK, ROWLOCK)
        SET
            ConsultantProfileId = @consultantProfileId,
            IsDeleted = 0,
            DeletedAt = NULL,
            PickUp = 1,
            AssignedAt = GETUTCDATE(),
            CallDeadlineAt = DATEADD(MINUTE, 3, GETUTCDATE()),
            CallInitiatedAt = NULL,
            LeadAssignmentState = @assignedState,
            AssignmentType = @realTimeType,
            RequiresThreeMinuteCall = 1,
            ReportDescription = CASE WHEN @sourceType = @burnedSource THEN NULL ELSE ReportDescription END,
            ReportSubmittedAt = CASE WHEN @sourceType = @burnedSource THEN NULL ELSE ReportSubmittedAt END,
            ContactedAt = CASE WHEN @sourceType = @burnedSource THEN NULL ELSE ContactedAt END,
            CallResult = CASE WHEN @sourceType = @burnedSource THEN NULL ELSE CallResult END
        WHERE Id = @leadAssignmentId
          AND ((@sourceType = @newSource AND IsDeleted = 0 AND ConsultantProfileId IS NULL
                AND PickUp = 0 AND ReportSubmittedAt IS NULL AND LeadAssignmentState = @newState)
            OR (@sourceType = @burnedSource AND AssignmentType = @realTimeType
                AND ((ConsultantProfileId IS NULL AND PickUp = 0) OR (ConsultantProfileId IS NOT NULL
                    AND CallResult = @noAnswerResult AND ReportSubmittedAt IS NOT NULL))))
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
                        (int)LeadAssignmentState.Assigned),
                    new SqlParameter("@newState", (int)LeadAssignmentState.New),
                    new SqlParameter("@noAnswerResult", (int)LeadCallResult.NoAnswer),
                    new SqlParameter("@realTimeType", (int)LeadAssignmentType.RealTime),
                    new SqlParameter("@sourceType", (int)sourceType),
                    new SqlParameter("@newSource", (int)LeadAssignmentSourceType.NewLeads),
                    new SqlParameter("@burnedSource", (int)LeadAssignmentSourceType.BurnedLeads)
                );

            return affectedRows == 1;
        }
    }
}
