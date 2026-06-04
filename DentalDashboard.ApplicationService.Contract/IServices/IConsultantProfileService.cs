namespace DentalDashboard.ApplicationService.Contract.IServices
{
    public interface IConsultantProfileService
    {
        Task SetOnlineStatusAsync(long consultantProfileId, bool isOnline);
        Task AssignOfflineQueueAsync();
        Task SetPresentStatusAsync(long consultantProfileId, bool isPresent);
    }
}
