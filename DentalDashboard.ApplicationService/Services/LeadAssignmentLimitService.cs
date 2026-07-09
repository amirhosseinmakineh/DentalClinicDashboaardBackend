using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.Domain.IRepositories;

namespace DentalDashboard.ApplicationService.Services
{
    public class LeadAssignmentLimitService : ILeadAssignmentLimitService
    {
        private readonly ILeadAssignmentRepository _repository;

        private const int DailyLimit = 10;

        public LeadAssignmentLimitService(
            ILeadAssignmentRepository repository)
        {
            _repository = repository;
        }


        public async Task<bool> CanPickupLeadAsync(long consultantProfileId)
        {
            var count = await _repository
                .GetTodayPickupCountAsync(consultantProfileId);

            return count < DailyLimit;
        }
    }
}
