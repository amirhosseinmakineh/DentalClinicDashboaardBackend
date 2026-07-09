using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Services
{
    public class ConsultantProfileService : IConsultantProfileService
    {
        private readonly IConsultantProfileRepository repository;
        private readonly ILeadAssignmentRepository leadAssignmentRepository;

        public ConsultantProfileService(
            IConsultantProfileRepository repository,
            ILeadAssignmentRepository leadAssignmentRepository)
        {
            this.repository = repository;
            this.leadAssignmentRepository = leadAssignmentRepository;
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
                consultant.IsOnline = false;
                consultant.LastOfflineAt = DateTime.Now;
            }

            await repository.SaveChange();
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
            }

            await repository.SaveChange();
        }
    }
}
