using DentalDashboard.Domain.Models;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Framwork.IRepositories;

namespace DentalDashboard.Domain.IRepositories
{
    public interface IConsultantProfileRepository : IBaseRepository<long, ConsultantProfile>
    {
        Task<List<ConsultantProfile>> GetAvailableConsultantsAsync();
        Task<List<ConsultantProfile>> GetOnlineConsultantsReadyForRealTimeAsync();
        Task<bool> HasOnlineConsultantAsync();
        Task<bool> TryReserveForPickupAsync(
            long consultantProfileId,
            DateTime reservedAtUtc,
            CancellationToken cancellationToken);
        Task<List<ConsultantProfile>> GetTestConsultantsReadyForDistributionAsync();
        Task<List<ConsultantProfile>> GetTestConsultantsReadyForEvaluationAsync(DateTime evaluationStartedBefore);
        Task<List<ConsultantProfile>> GetActiveSellerConsultantsAsync();
        Task<List<ConsultantProfile>> GetSellerConsultantsReadyForEvaluationAsync(DateTime evaluationStartedBefore);
        Task<bool> TryCompleteSellerEvaluationAsync(long consultantProfileId, DateTime evaluatedAt);
        Task<List<ConsultantProfile>> GetActiveTopSellerConsultantsAsync();
        Task<List<ConsultantProfile>> GetTopSellerConsultantsReadyForEvaluationAsync(DateTime startedBefore);
        Task<bool> TryCompleteTopSellerEvaluationAsync(
            long consultantProfileId,
            DateTime periodStart,
            DateTime evaluatedAt,
            DateTime nextPeriodStart,
            TopSellerRewardLevel rewardLevel);
    }
}
