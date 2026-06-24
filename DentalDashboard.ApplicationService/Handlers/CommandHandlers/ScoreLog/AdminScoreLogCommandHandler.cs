using DentalDashboard.ApplicationService.Contract.Requests.ScoreLog.Commands;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.ScoreLog
{
    public class AdminScoreLogCommandHandler : ICommandHandler<ScoreLogCommand>
    {
        private readonly IConsultantProfileRepository profileRepository;
        private readonly IScoreLogRepository scoreLogRepository;

        public AdminScoreLogCommandHandler(
            IConsultantProfileRepository profileRepository,
            IScoreLogRepository scoreLogRepository)
        {
            this.profileRepository = profileRepository;
            this.scoreLogRepository = scoreLogRepository;
        }

        public async Task<Result> HandleAsync(ScoreLogCommand command, CancellationToken cancellationToken = default)
        {
            if (command.Reason != ScoreReason.ManagerReward && command.Reason != ScoreReason.ManagerPenalty)
                return Result.Failure("فقط امتیاز تشویقی یا جریمه مدیر قابل ثبت است");

            if (command.Reason == ScoreReason.ManagerReward && command.ScoreValue < 0)
                return Result.Failure("امتیاز تشویقی مدیر باید مثبت باشد");

            if (command.Reason == ScoreReason.ManagerPenalty && command.ScoreValue > 0)
                return Result.Failure("امتیاز جریمه مدیر باید منفی باشد");

            var profile = await profileRepository.GetAll()
                .FirstOrDefaultAsync(x => x.Id == command.ConsultantProfileId, cancellationToken);

            if (profile == null)
                return Result.Failure("مشاوری یافت نشد");

            if (profile.IsDeleted)
                return Result.Failure("پروفایل مشاور حذف شده است");

            if (!profile.IsCompleteProfile)
                return Result.Failure("پروفایل مشاور کامل نیست");

            var score = new Domain.Models.ScoreLog()
            {
                ConsultantProfileId = command.ConsultantProfileId,
                CreatedAt = DateTime.UtcNow,
                DeletedAt = null,
                IsDeleted = false,
                LeadAssignmentId = command.LeadAssignmentId,
                Reason = command.Reason,
                ScoreValue = command.ScoreValue,
                Source = ScoreSource.Manager,
                UpdatedAt = null,
                Description = command.Description,
                CreatedByUserId = command.CreatedByUserId,
                UserId = profile.UserId,
            };

            profile.CurrentScore += command.ScoreValue;

            await scoreLogRepository.AddAsync(score);
            profileRepository.Update(profile);
            await scoreLogRepository.SaveChange();

            return Result.Success("امتیاز با موفقیت ثبت شد");
        }
    }
}
