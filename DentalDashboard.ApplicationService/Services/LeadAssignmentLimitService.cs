using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.Strategies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DentalDashboard.ApplicationService.Services
{
    public class LeadAssignmentLimitService : ILeadAssignmentLimitService
    {
        public const int SystemDefaultDailyLimit = 10;

        private readonly ILeadAssignmentRepository _repository;
        private readonly IConsultantProfileRepository _consultantProfileRepository;
        private readonly ILogger<LeadAssignmentLimitService> logger;

        public LeadAssignmentLimitService(
            ILeadAssignmentRepository repository,
            IConsultantProfileRepository consultantProfileRepository,
            ILogger<LeadAssignmentLimitService> logger)
        {
            _repository = repository;
            _consultantProfileRepository = consultantProfileRepository;
            this.logger = logger;
        }

        public int DefaultDailyLimit => SystemDefaultDailyLimit;

        public async Task<bool> CanPickupLeadAsync(long consultantProfileId)
        {
            return await CanPickupLeadAsync(consultantProfileId, burned: false);
        }

        public async Task<bool> CanPickupLeadAsync(long consultantProfileId, bool burned)
        {
            var profile = await _consultantProfileRepository.GetAll().AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == consultantProfileId && !x.IsDeleted);
            if (profile == null)
                return false;
            var policy = ConsultantDistributionPolicyResolver.Resolve(profile.ConsultantLevel);
            var limit = burned ? policy.BurnedDailyLimit : policy.RealTimeDailyLimit;
            if (limit <= 0)
                return false;
            var count = await _repository.GetTodayAssignmentCountAsync(consultantProfileId, burned);
            return count < limit;
        }

        public async Task<ConsultantDailyLimitStatus> GetDailyLimitStatusAsync(long consultantProfileId)
        {
            var profile = await _consultantProfileRepository.GetAll().AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == consultantProfileId && !x.IsDeleted);
            if (profile == null)
                return new ConsultantDailyLimitStatus { EffectiveDailyLimit = 0, TodayPickupCount = 0, CanPickup = false };

            var policy = ConsultantDistributionPolicyResolver.Resolve(profile.ConsultantLevel);
            var count = await _repository.GetTodayAssignmentCountAsync(consultantProfileId, burned: false);
            logger.LogDebug(
                "Role-based lead limits resolved for {ConsultantId}: {Role}, realtime {RealTimeLimit}, burned {BurnedLimit}, assigned {AssignedToday}",
                consultantProfileId, profile.ConsultantLevel, policy.RealTimeDailyLimit,
                policy.BurnedDailyLimit, count);

            return new ConsultantDailyLimitStatus
            {
                EffectiveDailyLimit = policy.RealTimeDailyLimit,
                TodayPickupCount = count,
                CanPickup = policy.AllowsRealTime && count < policy.RealTimeDailyLimit
            };
        }
    }
}
