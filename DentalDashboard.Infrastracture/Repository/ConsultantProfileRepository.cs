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
                .Where(x => !x.IsDeleted && x.User.IsActive &&
                            x.ConsultantLevel != ConsultantLevel.Test &&
                            x.IsCompleteProfile &&
                            x.IsAvailable &&
                            !x.IsOnline)
                .OrderBy(x => x.Id)
                .ToListAsync();
        }

        public Task<List<ConsultantProfile>> GetOnlineConsultantsReadyForRealTimeAsync()
        {
            return GetAll()
                .Where(x => !x.IsDeleted && x.User.IsActive &&
                            x.ConsultantLevel != ConsultantLevel.Test &&
                            x.IsCompleteProfile &&
                            x.IsAvailable &&
                            x.IsOnline &&
                            !x.CallAssignments.Any(l => !l.IsDeleted &&
                                                        l.ReportSubmittedAt == null &&
                                                        l.LeadAssignmentState != LeadAssignmentState.Expired &&
                                                        l.LeadAssignmentState != LeadAssignmentState.Rejected) &&
                            x.CallAssignments.Count(l => !l.IsDeleted &&
                                                         l.ReportSubmittedAt != null &&
                                                         l.CallResult.HasValue &&
                                                         l.CallResult.Value == LeadCallResult.NeedFollowUp &&
                                                         l.LeadAssignmentState == LeadAssignmentState.Pending) <= 10 &&
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

        public Task<List<ConsultantProfile>> GetTestConsultantsReadyForDistributionAsync()
        {
            return GetAll().Include(x => x.User)
                .Where(x => !x.IsDeleted && x.User.IsActive && x.IsCompleteProfile &&
                            x.IsAvailable && x.IsOnline &&
                            x.ConsultantLevel == ConsultantLevel.Test &&
                            x.TestStartedAt != null && x.TestCompletedAt == null &&
                            !x.CallAssignments.Any(l => l.ConsultantProfileId == x.Id &&
                                                        l.ReportSubmittedAt == null &&
                                                        l.LeadAssignmentState != LeadAssignmentState.Expired &&
                                                        l.LeadAssignmentState != LeadAssignmentState.Rejected) &&
                            x.CallAssignments.Count(l => l.ConsultantProfileId == x.Id &&
                                                         l.ReportSubmittedAt != null &&
                                                         l.CallResult == LeadCallResult.NeedFollowUp &&
                                                         l.LeadAssignmentState == LeadAssignmentState.Pending) <= 10)
                .OrderBy(x => x.Id).ToListAsync();
        }

        public Task<List<ConsultantProfile>> GetTestConsultantsReadyForEvaluationAsync(DateTime evaluationStartedBefore)
        {
            return GetAll().Include(x => x.User)
                .Where(x => !x.IsDeleted && x.ConsultantLevel == ConsultantLevel.Test &&
                            x.TestStartedAt != null && x.TestStartedAt < evaluationStartedBefore &&
                            x.TestCompletedAt == null)
                .OrderBy(x => x.Id).ToListAsync();
        }

        public Task<List<ConsultantProfile>> GetActiveSellerConsultantsAsync() => GetAll()
            .Include(x => x.User)
            .Where(x => !x.IsDeleted && x.User.IsActive && x.IsCompleteProfile &&
                        x.IsAvailable && x.IsOnline && x.ConsultantLevel == ConsultantLevel.Seller &&
                        x.SellerStartedAt != null)
            .OrderBy(x => x.Id).ToListAsync();

        public Task<List<ConsultantProfile>> GetSellerConsultantsReadyForEvaluationAsync(DateTime evaluationStartedBefore) =>
            GetAll().Include(x => x.User)
                .Where(x => !x.IsDeleted && x.ConsultantLevel == ConsultantLevel.Seller &&
                            x.SellerStartedAt != null && x.SellerStartedAt < evaluationStartedBefore &&
                            x.SellerEvaluatedAt == null)
                .OrderBy(x => x.Id).ToListAsync();

        public async Task<bool> TryCompleteSellerEvaluationAsync(long consultantProfileId, DateTime evaluatedAt) =>
            await GetAll().Where(x => x.Id == consultantProfileId && x.SellerEvaluatedAt == null)
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.SellerEvaluatedAt, evaluatedAt)) == 1;

        public Task<List<ConsultantProfile>> GetActiveTopSellerConsultantsAsync() => GetAll().AsNoTracking()
            .Include(x => x.User)
            .Where(x => !x.IsDeleted && x.User.IsActive && x.IsCompleteProfile &&
                        x.IsAvailable && x.IsOnline && x.ConsultantLevel == ConsultantLevel.TopSeller &&
                        x.TopSellerStartedAt != null)
            .OrderBy(x => x.Id).ToListAsync();

        public Task<List<ConsultantProfile>> GetTopSellerConsultantsReadyForEvaluationAsync(DateTime startedBefore) =>
            GetAll().Include(x => x.User)
                .Where(x => !x.IsDeleted && x.ConsultantLevel == ConsultantLevel.TopSeller &&
                            x.TopSellerStartedAt != null && x.TopSellerStartedAt < startedBefore)
                .OrderBy(x => x.Id).ToListAsync();

        public async Task<bool> TryCompleteTopSellerEvaluationAsync(
            long consultantProfileId, DateTime periodStart, DateTime evaluatedAt,
            DateTime nextPeriodStart, TopSellerRewardLevel rewardLevel) =>
            await GetAll().Where(x => x.Id == consultantProfileId &&
                                      x.ConsultantLevel == ConsultantLevel.TopSeller &&
                                      x.TopSellerStartedAt == periodStart &&
                                      x.TopSellerLastEvaluatedPeriodStart != periodStart)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.TopSellerLastEvaluatedPeriodStart, periodStart)
                    .SetProperty(x => x.TopSellerLastEvaluatedAt, evaluatedAt)
                    .SetProperty(x => x.TopSellerRewardLevel, rewardLevel)
                    .SetProperty(x => x.TopSellerStartedAt, nextPeriodStart)) == 1;
    }
}
