using DentalDashboard.Domain.Enums;

namespace DentalDashboard.ApplicationService.Contract.IServices
{
    public interface ILeadAssignmentLimitService
    {
        int DefaultDailyLimit { get; }

        Task<bool> CanPickupLeadAsync(long consultantProfileId, LeadLimitType leadType);

        Task<ConsultantDailyLimitStatus> GetDailyLimitStatusAsync(long consultantProfileId, LeadLimitType leadType);
        Task<ConsultantDailyLimitsStatus> GetDailyLimitsStatusAsync(long consultantProfileId);
        Task SetTestConsultantLimit(long consultantProfileId);
        Task SetSellerConsultantLimit(long consultantProfileId);
        Task SetTopSellerConsultantLimit(long consultantProfileId);
    }
}
