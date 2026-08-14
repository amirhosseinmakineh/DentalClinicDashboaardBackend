using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Services
{
    public class LeadAssignmentLimitService : ILeadAssignmentLimitService
    {
        private readonly ILeadAssignmentRepository _repository;
        private readonly IConsultantProfileRepository _consultantProfileRepository;
        public LeadAssignmentLimitService(
            ILeadAssignmentRepository repository,
            IConsultantProfileRepository consultantProfileRepository)
        {
            _repository = repository;
            _consultantProfileRepository = consultantProfileRepository;
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
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == consultantProfileId);

            if (profile == null)
                return 0;

            return (profile.ConsultantRole, leadType) switch
            {
                (ConsultantRole.Test, LeadLimitType.Realtime) => 0,
                (ConsultantRole.Test, LeadLimitType.Burnt) => 20,
                (ConsultantRole.Seller, LeadLimitType.Realtime) => 10,
                (ConsultantRole.Seller, LeadLimitType.Burnt) => 20,
                (ConsultantRole.TopSeller, LeadLimitType.Realtime) => 20,
                (ConsultantRole.TopSeller, LeadLimitType.Burnt) => 0,
                _ => 0
            };
        }
    }
}
