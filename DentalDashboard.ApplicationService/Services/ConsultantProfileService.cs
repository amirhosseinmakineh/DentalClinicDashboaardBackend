using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IDomainService;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Models;
using Microsoft.EntityFrameworkCore;

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

        public async Task<long?> EnsureProfileExistsAsync(Guid userId)
        {
            var profile = await repository.GetAll()
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (profile is not null)
            {
                if (profile.IsDeleted)
                {
                    profile.IsDeleted = false;
                    profile.DeletedAt = null;
                    profile.UpdatedAt = DateTime.UtcNow;
                    repository.Update(profile);
                    await repository.SaveChange();
                }

                return profile.Id;
            }

            profile = new ConsultantProfile
            {
                UserId = userId,
                NationalCode = string.Empty,
                Address = string.Empty,
                IsCompleteProfile = false,
                IsAvailable = false,
                IsOnline = false,
                CreatedAt = DateTime.UtcNow,
                WorkStartTime = TimeSpan.Zero,
                WorkEndTime = TimeSpan.Zero
            };

            await repository.AddAsync(profile);
            await repository.SaveChange();

            return profile.Id;
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

                var hasActiveRealTimeLead =
                    await leadAssignmentRepository
                        .HasActiveRealTimeLeadAsync(consultantProfileId);

                if (hasActiveRealTimeLead)
                    throw new InvalidOperationException("ابتدا تکلیف لید لحظه‌ای قبلی را مشخص کنید.");

                consultant.IsOnline = true;
                consultant.LastOnlineAt = DateTime.Now;
            }
            else
            {
                var now = DateTime.Now;
                consultant.IsOnline = false;
                consultant.LastOfflineAt = now;

                await AssignOfflineQueueToConsultantAsync(consultant, now);
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
                var now = DateTime.Now;
                consultant.IsAvailable = false;
                consultant.IsOnline = false;
                consultant.WorkEndTime = now.TimeOfDay;
                consultant.LastOfflineAt = now;

                await AssignOfflineQueueToConsultantAsync(consultant, now);
            }

            await repository.SaveChange();
        }
        private async Task AssignOfflineQueueToConsultantAsync(ConsultantProfile consultant, DateTime assignedAt)
        {
            var dailyAssignedCounts = await leadAssignmentRepository.GetDailyAssignedOfflineLeadCountsAsync(
                new[] { consultant.Id },
                assignedAt);

            var remainingCapacity = Math.Max(5 - dailyAssignedCounts.GetValueOrDefault(consultant.Id), 0);
            if (remainingCapacity <= 0)
                return;

            var leads = await leadAssignmentRepository.GetPendingOfflineLeadsAsync(remainingCapacity);
            if (!leads.Any())
                return;

            foreach (var lead in leads)
            {
                lead.ConsultantProfileId = consultant.Id;
                lead.AssignedAt = assignedAt;
                lead.LeadAssignmentState = LeadAssignmentState.Assigned;
                lead.AssignmentType = LeadAssignmentType.OfflineQueue;
                lead.RequiresThreeMinuteCall = false;
                lead.CallDeadlineAt = null;
            }
        }

    }
}
