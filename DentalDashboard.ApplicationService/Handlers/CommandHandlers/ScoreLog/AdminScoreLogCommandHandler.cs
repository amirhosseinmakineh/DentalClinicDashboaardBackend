using DentalDashboard.ApplicationService.Contract.Requests.ScoreLog.Commands;
using DentalDashboard.Domain.IDomainService;
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
        private readonly IConsultantScoreDomainService consultantScoreDomainService;

        public AdminScoreLogCommandHandler(
            IConsultantProfileRepository profileRepository,
            IScoreLogRepository scoreLogRepository,
            IConsultantScoreDomainService consultantScoreDomainService)
        {
            this.profileRepository = profileRepository;
            this.scoreLogRepository = scoreLogRepository;
            this.consultantScoreDomainService = consultantScoreDomainService;
        }

        public async Task<Result> HandleAsync(ScoreLogCommand command, CancellationToken cancellationToken = default)
        {
            if (command.Reason != ScoreReason.ManagerReward && command.Reason != ScoreReason.ManagerPenalty)
                return Result.Failure("فقط امتیاز تشویقی یا جریمه مدیر قابل ثبت است");

            if (command.ScoreValue < 0 || command.ScoreValue > 100)
                return Result.Failure("امتیاز مدیر باید بین ۰ تا ۱۰۰ باشد");

            var profile = await profileRepository.GetAll()
                .Include(x => x.ScoreLogs)
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

            consultantScoreDomainService.ApplyScoreEvent(profile, score);

            await scoreLogRepository.AddAsync(score);
            profileRepository.Update(profile);
            await scoreLogRepository.SaveChange();

            return Result.Success("امتیاز با موفقیت ثبت شد");
        }
    }
}
