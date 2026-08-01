using DentalDashboard.Domain.Enums;

namespace DentalDashboard.ApplicationService.Contract.IServices
{
    public interface IConsultantProfileService
    {
        Task<long?> EnsureProfileExistsAsync(Guid userId);
        Task SetConsultantLevelAsync(Guid userId, ConsultantLevel consultantLevel);
        Task SetOnlineStatusAsync(long consultantProfileId, bool isOnline);
        Task SetPresentStatusAsync(long consultantProfileId, bool isPresent);
    }
}
