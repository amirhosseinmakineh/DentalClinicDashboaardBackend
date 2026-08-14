using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IRepositories;
using Microsoft.EntityFrameworkCore;
using DentalDashboard.Domain.RolePolicies;

namespace DentalDashboard.ApplicationService.Services
{
    public class LeadAssignmentLimitService : ILeadAssignmentLimitService
    {
        private readonly ILeadAssignmentRepository _repository;
        private readonly IConsultantProfileRepository _consultantProfileRepository;
        private readonly IConsultantRolePolicyProvider _policyProvider;
        public LeadAssignmentLimitService(
            ILeadAssignmentRepository repository,
            IConsultantProfileRepository consultantProfileRepository,
            IConsultantRolePolicyProvider policyProvider)
        {
            _repository = repository;
            _consultantProfileRepository = consultantProfileRepository;
            _policyProvider = policyProvider;
        }

        public async Task<bool> CanPickupLeadAsync(long consultantProfileId, LeadLimitType leadType)
        {
            var status = await GetDailyLimitStatusAsync(
                consultantProfileId,
                leadType);

            return status.CanPickup;
        }

        public async Task<ConsultantDailyLimitStatus> GetDailyLimitStatusAsync(long consultantProfileId, LeadLimitType leadType)
        {
            var effectiveLimit = await GetEffectiveDailyLimitAsync(
                consultantProfileId,
                leadType);

            if (effectiveLimit <= 0)
            {
                return new ConsultantDailyLimitStatus
                {
                    EffectiveDailyLimit = effectiveLimit,
                    TodayPickupCount = 0,
                    CanPickup = false
                };
            }

            var count = await _repository.GetTodayPickupCountAsync(
                consultantProfileId,
                leadType);

            return new ConsultantDailyLimitStatus
            {
                EffectiveDailyLimit = effectiveLimit,
                TodayPickupCount = count,
                CanPickup = count < effectiveLimit
            };
        }

        public async Task<ConsultantDailyLimitsStatus> GetDailyLimitsStatusAsync(long consultantProfileId)
        {
            var role = await _consultantProfileRepository
                .GetAll()
                .AsNoTracking()
                .Where(x => x.Id == consultantProfileId)
                .Select(x => (ConsultantRole?)x.ConsultantRole)
                .FirstOrDefaultAsync();

            return role switch
            {
                ConsultantRole.Test => new ConsultantDailyLimitsStatus
                {
                    Burnt = await GetDailyLimitStatusAsync(consultantProfileId, LeadLimitType.Burnt)
                },
                ConsultantRole.Seller => new ConsultantDailyLimitsStatus
                {
                    Realtime = await GetDailyLimitStatusAsync(consultantProfileId, LeadLimitType.Realtime),
                    Burnt = await GetDailyLimitStatusAsync(consultantProfileId, LeadLimitType.Burnt)
                },
                ConsultantRole.TopSeller => new ConsultantDailyLimitsStatus
                {
                    Realtime = await GetDailyLimitStatusAsync(consultantProfileId, LeadLimitType.Realtime)
                },
                _ => new ConsultantDailyLimitsStatus()
            };
        }

        private async Task<int> GetEffectiveDailyLimitAsync(long consultantProfileId, LeadLimitType leadType)
        {
            var profile = await _consultantProfileRepository
                .GetAll()
                .Include(x => x.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == consultantProfileId);

            if (profile == null)
                return 0;

            if (!profile.User.IsActive || profile.IsDeleted)
                return 0;

            var periodStartedAt = profile.RoleStartedAt ?? profile.CreatedAt;
            return _policyProvider.GetDailyLimit(profile.ConsultantRole, leadType, periodStartedAt, DateTime.UtcNow);
        }
    }
}
