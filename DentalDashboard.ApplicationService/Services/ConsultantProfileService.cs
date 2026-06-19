using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.Domain.IDomainService;
using DentalDashboard.Domain.IRepositories;

namespace DentalDashboard.ApplicationService.Services
{
    public class ConsultantProfileService : IConsultantProfileService
    {
        private readonly IConsultantProfileRepository repository;
        private readonly ILeadAssignmentRepository leadAssignmentRepository;
        private readonly IOfflineLeadAssignmentStrategy offlineLeadAssignmentStrategy;

        public ConsultantProfileService(
            IConsultantProfileRepository repository,
            ILeadAssignmentRepository leadAssignmentRepository,
            IOfflineLeadAssignmentStrategy offlineLeadAssignmentStrategy)
        {
            this.repository = repository;
            this.leadAssignmentRepository = leadAssignmentRepository;
            this.offlineLeadAssignmentStrategy = offlineLeadAssignmentStrategy;
        }

        public async Task SetOnlineStatusAsync(long consultantProfileId, bool isOnline)
        {
            var consultant = await repository.GetByIdAsync(consultantProfileId);

            if (consultant == null)
                throw new InvalidOperationException("مشاور پیدا نشد.");

            if (consultant.IsDeleted)
                throw new InvalidOperationException("پروفایل مشاور حذف شده است.");

            if (!consultant.IsCompleteProfile)
                throw new InvalidOperationException("پروفایل مشاور کامل نیست.");

            if (!consultant.IsAvailable)
                throw new InvalidOperationException("مشاور فعال نیست.");

            if (isOnline)
            {
                var hasPendingOfflineLeads =
                    await leadAssignmentRepository
                        .HasPendingOfflineLeadsAsync(consultantProfileId);

                if (hasPendingOfflineLeads)
                    throw new InvalidOperationException("ابتدا لیدهای آفلاین خود را بررسی کنید.");

                consultant.IsOnline = true;
                consultant.LastOnlineAt = DateTime.Now;
            }
            else
            {
                consultant.IsOnline = false;
                consultant.LastOfflineAt = DateTime.Now;
            }

            await repository.SaveChange();
        }

        public async Task AssignOfflineQueueAsync()
        {
            var consultants = await repository.GetAvailableConsultantsForOfflineAssignmentAsync();
            if (!consultants.Any())
                return;

            var dailyAssignedCounts = await leadAssignmentRepository.GetDailyAssignedOfflineLeadCountsAsync(
                consultants.Select(x => x.Id),
                DateTime.Now);
            var totalRemainingDailyCapacity = consultants
                .Sum(x => Math.Max(5 - dailyAssignedCounts.GetValueOrDefault(x.Id), 0));
            if (totalRemainingDailyCapacity <= 0)
                return;

            var leads = await leadAssignmentRepository.GetPendingOfflineLeadsAsync(totalRemainingDailyCapacity);
            if (!leads.Any())
                return;

            offlineLeadAssignmentStrategy.Assign(leads, consultants, dailyAssignedCounts);
            await leadAssignmentRepository.SaveChange();
        }

        public async Task SetPresentStatusAsync(long consultantProfileId, bool isPresent)
        {
            var consultant = await repository.GetByIdAsync(consultantProfileId);

            if (consultant == null)
                throw new InvalidOperationException("مشاور پیدا نشد.");

            if (consultant.IsDeleted)
                throw new InvalidOperationException("پروفایل مشاور حذف شده است.");

            if (!consultant.IsCompleteProfile)
                throw new InvalidOperationException("پروفایل مشاور کامل نیست.");

            if (isPresent)
            {
                consultant.IsAvailable = true;
                consultant.WorkStartTime = DateTime.Now.TimeOfDay;
            }
            else
            {
                consultant.IsAvailable = false;
                consultant.IsOnline = false;
                consultant.WorkEndTime = DateTime.Now.TimeOfDay;
                consultant.LastOfflineAt = DateTime.Now;
            }

            await repository.SaveChange();
        }
    }
}
