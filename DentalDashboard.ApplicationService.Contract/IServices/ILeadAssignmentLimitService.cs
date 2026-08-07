namespace DentalDashboard.ApplicationService.Contract.IServices
{
    public interface ILeadAssignmentLimitService
    {
        int DefaultDailyLimit { get; }

        Task<bool> CanPickupLeadAsync(long consultantProfileId);
        Task<bool> CanPickupLeadAsync(long consultantProfileId, bool burned);

        Task<ConsultantDailyLimitStatus> GetDailyLimitStatusAsync(long consultantProfileId);
    }
}
