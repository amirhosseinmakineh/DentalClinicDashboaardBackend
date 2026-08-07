using DentalDashboard.Domain.Models;
using DentalDashboard.Framwork.IRepositories;

namespace DentalDashboard.Domain.IRepositories
{
    public interface IConsultantProfileRepository : IBaseRepository<long, ConsultantProfile>
    {
        Task<List<ConsultantProfile>> GetAvailableConsultantsAsync();
        Task<List<ConsultantProfile>> GetOnlineConsultantsReadyForRealTimeAsync();
        Task<bool> HasOnlineConsultantAsync();
        Task<List<ConsultantProfile>> GetTestConsultantsReadyForDistributionAsync();
        Task<List<ConsultantProfile>> GetTestConsultantsReadyForEvaluationAsync(DateTime evaluationStartedBefore);
        Task<List<ConsultantProfile>> GetActiveSellerConsultantsAsync();
        Task<List<ConsultantProfile>> GetSellerConsultantsReadyForEvaluationAsync(DateTime evaluationStartedBefore);
        Task<bool> TryCompleteSellerEvaluationAsync(long consultantProfileId, DateTime evaluatedAt);
    }
}
