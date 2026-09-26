using DentalDashboard.ApplicationService.Contract.Dtos.Consultant;

namespace DentalDashboard.ApplicationService.Contract.IServices
{
    public interface IConsultantProfileService
    {
        Task<long?> EnsureProfileExistsAsync(Guid userId);
        Task SetOnlineStatusAsync(long consultantProfileId, bool isOnline);
        Task SetPresentStatusAsync(long consultantProfileId, bool isPresent);
    }
}
