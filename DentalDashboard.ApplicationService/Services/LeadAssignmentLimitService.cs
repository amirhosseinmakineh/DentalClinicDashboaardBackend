using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Services
{
    public class LeadAssignmentLimitService : ILeadAssignmentLimitService
    {
        public const int SystemDefaultDailyLimit = 10;

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
            var status = await GetDailyLimitStatusAsync(consultantProfileId);
            return status.CanPickup;
        }

        public async Task<ConsultantDailyLimitStatus> GetDailyLimitStatusAsync(long consultantProfileId)
        {
            var effectiveLimit = await GetEffectiveDailyLimitAsync(consultantProfileId);
            var count = await _repository.GetTodayPickupCountAsync(consultantProfileId);

            return new ConsultantDailyLimitStatus
            {
                EffectiveDailyLimit = effectiveLimit,
                TodayPickupCount = count,
                CanPickup = count < effectiveLimit
            };
        }

        private async Task<int> GetEffectiveDailyLimitAsync(long consultantProfileId)
        {
            var profile = await _consultantProfileRepository.GetAll()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == consultantProfileId);

            // Zero was accepted by older versions when the admin form sent its
            // default numeric value. Treat it as "not configured" rather than
            // silently blocking the consultant from every lead.
            if (profile?.ConsultantLevel == ConsultantLevel.Test)
                return 20;

            return profile?.LimitNumber is > 0
                ? profile.LimitNumber.Value
                : SystemDefaultDailyLimit;
        }
    }
}
