namespace DentalDashboard.ApplicationService.Contract.IServices
{
    public interface ILeadAssignmentLimitService
    {
        int DefaultDailyLimit { get; }

        Task<bool> CanPickupLeadAsync(long consultantProfileId);

        Task<ConsultantDailyLimitStatus> GetDailyLimitStatusAsync(long consultantProfileId);
    }
}
