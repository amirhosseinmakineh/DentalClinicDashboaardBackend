using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Models;
using DentalDashboard.Infrastracture.Context;
using DentalDashboard.Utilities.Time;
using DentalDashboard.Domain.Strategies;
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

            // Always dispatch every newly queued phone number once before sending
            // reminders for numbers which have already been announced. Previously
            // an in-flight lead was selected first forever, so it was reminded on
            // every cycle while later phone numbers never reached the frontend.
            var undispatchedLead = await baseQuery
                .Where(x => !x.NotificationSent)
                .OrderBy(x => x.CreatedAt)
                .ThenBy(x => x.Id)
                .FirstOrDefaultAsync();

            if (undispatchedLead != null)
                return undispatchedLead;

            return await baseQuery
                .Where(x => x.NotificationSent)
                .OrderBy(x => x.LastDispatchAt ?? x.CreatedAt)
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
            var burnedLevels = Enum.GetValues<ConsultantLevel>()
                .Where(x => ConsultantDistributionPolicyResolver.Resolve(x).AllowsBurned)
                .ToArray();
            return GetAll()
                .FirstOrDefaultAsync(x => x.Id == leadAssignmentId &&
                                          x.ConsultantProfileId == consultantProfileId &&
                                          (!x.IsDeleted || burnedLevels.Contains(x.ConsultantProfile!.ConsultantLevel)));
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
            // AssignedAt is persisted as UTC (see TryPickupLeadAsync), while the
            // business definition of "today" is the Iran calendar day. Using the
            // server's local date here caused leads picked up during the previous
            // Iran evening to count against the new day's limit on UTC servers.
            var todayInIran = IranTimeHelper.TodayInIran();
            var (todayStartUtc, _) =
                IranTimeHelper.GetIranDayRangeAsUtc(todayInIran);
            var (tomorrowStartUtc, _) =
                IranTimeHelper.GetIranDayRangeAsUtc(todayInIran.AddDays(1));

            return await context.LeadAssignments
                .CountAsync(x =>
                    x.ConsultantProfileId == consultantProfileId &&
                    x.PickUp &&
                    x.AssignedAt >= todayStartUtc &&
                    x.AssignedAt < tomorrowStartUtc);
        }

        public async Task<(int NewLeadCount, int BurnedLeadCount)> GetSellerDailyAllocationCountAsync(
            long consultantProfileId, CancellationToken cancellationToken = default)
        {
            var today = IranTimeHelper.TodayInIran();
            var (start, _) = IranTimeHelper.GetIranDayRangeAsUtc(today);
            var (end, _) = IranTimeHelper.GetIranDayRangeAsUtc(today.AddDays(1));
            var counts = await context.LeadAssignments
                .Where(x => x.ConsultantProfileId == consultantProfileId && x.PickUp &&
                            x.AssignedAt >= start && x.AssignedAt < end)
                .GroupBy(_ => 1)
                .Select(g => new { New = g.Count(x => !x.IsDeleted), Burned = g.Count(x => x.IsDeleted) })
                .SingleOrDefaultAsync(cancellationToken);
            return counts == null ? (0, 0) : (counts.New, counts.Burned);
        }

        public async Task<int> GetTodayAssignmentCountAsync(
            long consultantProfileId, bool burned, CancellationToken cancellationToken = default)
        {
            var today = IranTimeHelper.TodayInIran();
            var (start, _) = IranTimeHelper.GetIranDayRangeAsUtc(today);
            var (end, _) = IranTimeHelper.GetIranDayRangeAsUtc(today.AddDays(1));
            return await context.LeadAssignments.CountAsync(x =>
                x.ConsultantProfileId == consultantProfileId && x.PickUp &&
                x.IsDeleted == burned && x.AssignedAt >= start && x.AssignedAt < end,
                cancellationToken);
        }

        public async Task<bool> TryPickupLeadAsync(
            long leadAssignmentId,
            long consultantProfileId,
            CancellationToken cancellationToken)
        {
            var todayInIran = IranTimeHelper.TodayInIran();
            var (todayStartUtc, _) = IranTimeHelper.GetIranDayRangeAsUtc(todayInIran);
            var (tomorrowStartUtc, _) = IranTimeHelper.GetIranDayRangeAsUtc(todayInIran.AddDays(1));
            var testPolicy = ConsultantDistributionPolicyResolver.Resolve(ConsultantLevel.Test);
            var sellerPolicy = ConsultantDistributionPolicyResolver.Resolve(ConsultantLevel.Seller);
            var topSellerPolicy = ConsultantDistributionPolicyResolver.Resolve(ConsultantLevel.TopSeller);
            var sql = @"
UPDATE LeadAssignments
SET
    ConsultantProfileId = @consultantProfileId,
    PickUp = 1,
    AssignedAt = GETUTCDATE(),
    CallDeadlineAt = DATEADD(MINUTE, 3, GETUTCDATE()),
    LeadAssignmentState = @assignedState
WHERE
    Id = @leadAssignmentId
    AND ConsultantProfileId IS NULL
    AND PickUp = 0
    AND AssignmentType = @realTimeType
    AND LeadAssignmentState = @newState
    AND ReportSubmittedAt IS NULL

    AND EXISTS
    (
        SELECT 1
        FROM ConsultantProfiles AS c
        INNER JOIN Users AS u
            ON u.Id = c.UserId

        WHERE
            c.Id = @consultantProfileId
            AND c.IsDeleted = 0
            AND c.IsCompleteProfile = 1
            AND c.IsAvailable = 1
            AND c.IsOnline = 1
            AND u.IsActive = 1

            AND
            (
                ------------------------------------------------------------
                -- Top Seller
                ------------------------------------------------------------
                (
                    c.ConsultantLevel = @topSellerLevel
                    AND c.TopSellerStartedAt IS NOT NULL
                    AND LeadAssignments.IsDeleted = 0

                    AND
                    (
                        SELECT COUNT(1)
                        FROM LeadAssignments AS daily
                             WITH (UPDLOCK, HOLDLOCK)

                        WHERE
                            daily.ConsultantProfileId = c.Id
                            AND daily.PickUp = 1
                            AND daily.IsDeleted = 0
                            AND daily.AssignedAt >= @todayStartUtc
                            AND daily.AssignedAt < @tomorrowStartUtc
                    ) < @topSellerRealTimeLimit
                )

                OR

                ------------------------------------------------------------
                -- Seller
                ------------------------------------------------------------
                (
                    c.ConsultantLevel = @sellerLevel
                    AND c.SellerStartedAt IS NOT NULL

                    AND
                    (
                        ----------------------------------------------------
                        -- Seller / RealTime
                        ----------------------------------------------------
                        (
                            LeadAssignments.IsDeleted = 0

                            AND
                            (
                                SELECT COUNT(1)
                                FROM LeadAssignments AS daily
                                     WITH (UPDLOCK, HOLDLOCK)

                                WHERE
                                    daily.ConsultantProfileId = c.Id
                                    AND daily.PickUp = 1
                                    AND daily.IsDeleted = 0
                                    AND daily.AssignedAt >= @todayStartUtc
                                    AND daily.AssignedAt < @tomorrowStartUtc
                            ) < @sellerRealTimeLimit
                        )

                        OR

                        ----------------------------------------------------
                        -- Seller / Burned
                        ----------------------------------------------------
                        (
                            LeadAssignments.IsDeleted = 1

                            AND
                            (
                                SELECT COUNT(1)
                                FROM LeadAssignments AS daily
                                     WITH (UPDLOCK, HOLDLOCK)

                                WHERE
                                    daily.ConsultantProfileId = c.Id
                                    AND daily.PickUp = 1
                                    AND daily.IsDeleted = 1
                                    AND daily.AssignedAt >= @todayStartUtc
                                    AND daily.AssignedAt < @tomorrowStartUtc
                            ) < @sellerBurnedLimit
                        )
                    )
                )

                OR

                ------------------------------------------------------------
                -- Test Consultant
                ------------------------------------------------------------
                (
                    c.ConsultantLevel = @testLevel
                    AND LeadAssignments.IsDeleted = 1
                    AND c.TestStartedAt IS NOT NULL

                    AND
                    DATEDIFF
                    (
                        DAY,

                        CAST
                        (
                            DATEADD
                            (
                                MINUTE,
                                210,
                                CAST(c.TestStartedAt AS datetime2)
                            )
                            AS date
                        ),

                        CAST
                        (
                            DATEADD
                            (
                                MINUTE,
                                210,
                                CAST(GETUTCDATE() AS datetime2)
                            )
                            AS date
                        )
                    ) BETWEEN 0 AND 4

                    AND
                    (
                        SELECT COUNT(1)
                        FROM LeadAssignments AS daily
                             WITH (UPDLOCK, HOLDLOCK)

                        WHERE
                            daily.ConsultantProfileId = c.Id
                            AND daily.PickUp = 1
                            AND daily.IsDeleted = 1
                            AND daily.AssignedAt >= @todayStartUtc
                            AND daily.AssignedAt < @tomorrowStartUtc
                    ) < @testBurnedLimit
                )
            )
    );
";

            var parameters = new object[]
            {
                    new SqlParameter(
                        "@consultantProfileId",
                        consultantProfileId),
                    new SqlParameter(
                        "@leadAssignmentId",
                        leadAssignmentId),
                    new SqlParameter(
                        "@assignedState",
                        (int)LeadAssignmentState.Assigned),
                    new SqlParameter("@testLevel", (byte)ConsultantLevel.Test),
                    new SqlParameter("@sellerLevel", (byte)ConsultantLevel.Seller),
                    new SqlParameter("@topSellerLevel", (byte)ConsultantLevel.TopSeller),
                    new SqlParameter("@testBurnedLimit", testPolicy.BurnedDailyLimit),
                    new SqlParameter("@sellerRealTimeLimit", sellerPolicy.RealTimeDailyLimit),
                    new SqlParameter("@sellerBurnedLimit", sellerPolicy.BurnedDailyLimit),
                    new SqlParameter("@topSellerRealTimeLimit", topSellerPolicy.RealTimeDailyLimit),
                    new SqlParameter("@realTimeType", (int)LeadAssignmentType.RealTime),
                    new SqlParameter("@newState", (int)LeadAssignmentState.New),
                    new SqlParameter("@todayStartUtc", todayStartUtc),
                    new SqlParameter("@tomorrowStartUtc", tomorrowStartUtc)
            };
            var affectedRows = await context.Database.ExecuteSqlRawAsync(sql, parameters, cancellationToken);

            return affectedRows == 1;
        }

        public async Task<LeadAssignment?> GetCurrentBurnedLeadForDispatchAsync(TimeSpan redispatchInterval)
        {
            var redispatchBefore = DateTime.UtcNow.Subtract(redispatchInterval);
            var query = GetAll().Where(x => x.IsDeleted &&
                                            x.AssignmentType == LeadAssignmentType.RealTime &&
                                            x.ConsultantProfileId == null &&
                                            x.ReportSubmittedAt == null &&
                                            x.LeadAssignmentState == LeadAssignmentState.New &&
                                            !x.PickUp);

            var fresh = await query.Where(x => !x.NotificationSent)
                .OrderBy(x => x.CreatedAt).ThenBy(x => x.Id).FirstOrDefaultAsync();
            if (fresh != null)
                return fresh;

            return await query.Where(x => x.NotificationSent &&
                                          (x.LastDispatchAt == null || x.LastDispatchAt < redispatchBefore))
                .OrderBy(x => x.LastDispatchAt ?? x.CreatedAt).ThenBy(x => x.Id).FirstOrDefaultAsync();
        }
    }
}
