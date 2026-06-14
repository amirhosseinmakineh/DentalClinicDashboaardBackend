using DentalDashboard.ApplicationService.Contract.Requests.ScoreLog.Commands;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;
using System.Windows.Input;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.ScoreLog
{
    public class AdminScoreLogCommandHandler : ICommandHandler<ScoreLogCommand>
    {
        private readonly IUserRepository userRepository;
        private readonly IConsultantProfileRepository profileRepository;
        private readonly IScoreLogRepository scoreLogRepository;
        public AdminScoreLogCommandHandler(IUserRepository userRepository, IConsultantProfileRepository profileRepository, IScoreLogRepository scoreLogRepository)
        {
            this.userRepository = userRepository;
            this.profileRepository = profileRepository;
            this.scoreLogRepository = scoreLogRepository;
        }

        public async Task<Result> HandleAsync(ScoreLogCommand command, CancellationToken cancellationToken = default)
        {
            var consultant = profileRepository.GetAll()
                .Include(x => x.User)
                .Where(x => x.Id == command.ConsultantProfileId).FirstOrDefault();
            if (consultant is not null)
            {
                var score = new global::ScoreLog()
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
                    UserId = consultant.User.Id,
                    
                };
                await scoreLogRepository.AddAsync(score);
                await scoreLogRepository.SaveChange();
                return Result<string>.Success("ثبت امتیاز با موفقیت انجام شد");
            }
            else
                return Result<string>.Failure("ثبت امتیاز با خطا مواجه شد");
        }
    }
}
