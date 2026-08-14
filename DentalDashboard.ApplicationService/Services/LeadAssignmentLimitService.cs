using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IRepositories;
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

        public async Task<bool> CanPickupLeadAsync(long consultantProfileId,LeadAssignmentType leadType)
        {
            var status = await GetDailyLimitStatusAsync(
                consultantProfileId,
                leadType);

            return status.CanPickup;
        }

        public async Task<ConsultantDailyLimitStatus> GetDailyLimitStatusAsync(long consultantProfileId,LeadAssignmentType leadType)
        {
            var effectiveLimit = await GetEffectiveDailyLimitAsync(
                consultantProfileId,
                leadType);

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

        private async Task<int> GetEffectiveDailyLimitAsync(long consultantProfileId,LeadAssignmentType leadType)
        {
            var profile = await _consultantProfileRepository
                .GetAll()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == consultantProfileId);

            if (profile == null)
                return 0;

            return leadType switch
            {
                LeadAssignmentType.RealTime => 10,
                LeadAssignmentType.Burnt => profile.LimitNumber ?? SystemDefaultDailyLimit,
                _ => 0
            };
        }
        public async Task SetSellerConsultantLimit(long consultantProfileId)
        {
            var consultant = await _consultantProfileRepository.GetAll().Where(x => x.Id == consultantProfileId).FirstOrDefaultAsync();
            if (consultant == null)
                throw new InvalidOperationException("مشاور پیدا نشد.");
            consultant.LimitNumber = 20;
            _consultantProfileRepository.Update(consultant);
            await _consultantProfileRepository.SaveChange();

        }

        public async Task SetTestConsultantLimit(long consultantProfileId)
        {
            var consultant = await _consultantProfileRepository.GetAll().Where(x => x.Id == consultantProfileId).FirstOrDefaultAsync();
            if (consultant == null)
                throw new InvalidOperationException("مشاور پیدا نشد.");
            consultant.LimitNumber = 40;
            _consultantProfileRepository.Update(consultant);
            await _consultantProfileRepository.SaveChange();
        }

        public async Task SetTopSellerConsultantLimit(long consultantProfileId)
        {
            var consultant = await _consultantProfileRepository.GetAll().Where(x => x.Id == consultantProfileId).FirstOrDefaultAsync();
            if (consultant == null)
                throw new InvalidOperationException("مشاور پیدا نشد.");
            consultant.LimitNumber = 30;
            _consultantProfileRepository.Update(consultant);
            await _consultantProfileRepository.SaveChange();
        }
    }
}
