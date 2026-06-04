using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.Domain.DomainServices;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IDomainService;
using DentalDashboard.Domain.IRepositories;

namespace DentalDashboard.ApplicationService.Services
{
    public class ConsultantProfileService : IConsultantProfileService
    {
        private readonly IConsultantProfileRepository repository;
        private readonly ILeadAssignmentRepository leadAssignmentRepository;
        private readonly ILeadAssignmentStrategy leadAssignmentStrategy;
        private readonly IOfflineLeadAssignmentStrategy offlineLeadAssignmentStrategy;
        public ConsultantProfileService(IConsultantProfileRepository repository, ILeadAssignmentRepository leadAssignmentRepository, ILeadAssignmentStrategy leadAssignmentStrategy, IOfflineLeadAssignmentStrategy offlineLeadAssignmentStrategy)
        {
            this.repository = repository;
            this.leadAssignmentRepository = leadAssignmentRepository;
            this.leadAssignmentStrategy = leadAssignmentStrategy;
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
            var offlineLeads = await leadAssignmentRepository
                .GetPendingOfflineQueueAsync();

            var leads = offlineLeads.ToList();

            if (!leads.Any())
                return;

            var consultants = await repository.GetAvailableConsultantsAsync();

            var presentConsultants = consultants
                .Where(x => x.IsAvailable)
                .ToList();

            if (!presentConsultants.Any())
                return;

            offlineLeadAssignmentStrategy.Assign(
                leads,
                presentConsultants
            );

            foreach (var lead in leads)
            {
                lead.LeadAssignmentState = LeadAssignmentState.Assigned;
                lead.AssignedAt = DateTime.Now;
                lead.RequiresThreeMinuteCall = false;
                lead.CallDeadlineAt = null;
            }

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

            if (!consultant.IsAvailable)
                throw new InvalidOperationException("مشاور فعال نیست.");

            if (isPresent)
            {
                consultant.IsAvailable = true;
                consultant.LastOnlineAt = DateTime.Now;

                await repository.SaveChange();

                await AssignOfflineQueueAsync();
            }
            else
            {
                consultant.IsAvailable = false;
                consultant.LastOfflineAt = DateTime.Now;

                consultant.IsOnline = false;
                consultant.LastOfflineAt = DateTime.Now;

                await repository.SaveChange();
            }
        }
    }
}
