using DentalDashboard.Domain.Models;
using DentalDashboard.Framwork.IRepositories;

namespace DentalDashboard.Domain.IRepositories
{
    public interface IConsultantProfileRepository : IBaseRepository<long, ConsultantProfile>
    {
        Task<List<ConsultantProfile>> GetAvailableConsultantsAsync();
        Task<List<ConsultantProfile>> GetOnlineConsultantsReadyForRealTimeAsync();
        Task<bool> HasOnlineConsultantAsync();
        Task<List<ConsultantProfile>> GetAvailableAndOnnlineTestConsultant();
        Task<List<ConsultantProfile>> GetAvailableAndOnnlineSellerConsultant();
        Task<List<ConsultantProfile>> GetAvailableAndOnnlineTopSellerConsultant();
    }
}
