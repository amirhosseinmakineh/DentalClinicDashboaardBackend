using DentalDashboard.Domain.Enums;

namespace DentalDashboard.ApplicationService.Contract.IServices
{
    public interface ILeadAssignmentLimitService
    {
        Task<bool> CanPickupLeadAsync(long consultantProfileId, LeadLimitType leadType);

        Task<ConsultantDailyLimitStatus> GetDailyLimitStatusAsync(long consultantProfileId, LeadLimitType leadType);
        Task<ConsultantDailyLimitsStatus> GetDailyLimitsStatusAsync(long consultantProfileId);
    }
}
