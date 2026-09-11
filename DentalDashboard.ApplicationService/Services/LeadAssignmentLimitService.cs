using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.Domain.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Services
{
    public class LeadAssignmentLimitService : ILeadAssignmentLimitService
    {
        public const int SystemDefaultDailyLimit = int.MaxValue;

        private readonly ILeadAssignmentRepository _repository;
        private readonly IConsultantProfileRepository _consultantProfileRepository;

        public LeadAssignmentLimitService(
            ILeadAssignmentRepository repository,
            IConsultantProfileRepository consultantProfileRepository)
        {
            _repository = repository;
            _consultantProfileRepository = consultantProfileRepository;
        }

        public int DefaultDailyLimit => SystemDefaultDailyLimit;

        public async Task<bool> CanPickupLeadAsync(long consultantProfileId)
        {
            await Task.CompletedTask;
            return true;
        }

        public async Task<ConsultantDailyLimitStatus> GetDailyLimitStatusAsync(long consultantProfileId)
        {
            var count = await _repository.GetTodayPickupCountAsync(consultantProfileId);

            return new ConsultantDailyLimitStatus
            {
                EffectiveDailyLimit = int.MaxValue,
                TodayPickupCount = count,
                CanPickup = true
            };
        }

        private async Task<int> GetEffectiveDailyLimitAsync(long consultantProfileId)
        {
            var profile = await _consultantProfileRepository.GetAll()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == consultantProfileId);

            return profile?.LimitNumber ?? SystemDefaultDailyLimit;
        }
    }
}
