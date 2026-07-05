using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.ApplicationService.Contract.Requests.Consultant.Commands;
using DentalDashboard.ApplicationService.Contract.Responses.LeadResponse;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IDomainService;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.Consultant
{
    public class ExpireLeadNoCallCommandHandler : ICommandHandler<ExpireLeadNoCallCommand, ExpireLeadNoCallResponse>
    {
        private const int NoCallPenaltyScore = -10;

        private readonly ILeadAssignmentRepository leadAssignmentRepository;
        private readonly IConsultantProfileRepository consultantProfileRepository;
        private readonly IScoreLogRepository scoreLogRepository;
        private readonly ILeadDomainService leadDomainService;
        private readonly ILeadAssignmentService leadAssignmentService;

        public ExpireLeadNoCallCommandHandler(
            ILeadAssignmentRepository leadAssignmentRepository,
            IConsultantProfileRepository consultantProfileRepository,
            IScoreLogRepository scoreLogRepository,
            ILeadDomainService leadDomainService,
            ILeadAssignmentService leadAssignmentService)
        {
            this.leadAssignmentRepository = leadAssignmentRepository;
            this.consultantProfileRepository = consultantProfileRepository;
            this.scoreLogRepository = scoreLogRepository;
            this.leadDomainService = leadDomainService;
            this.leadAssignmentService = leadAssignmentService;
        }

        public async Task<Result<ExpireLeadNoCallResponse>> HandleAsync(
            ExpireLeadNoCallCommand command,
            CancellationToken cancellationToken = default)
        {
            var lead = await leadAssignmentRepository.GetByIdAndConsultantAsync(
                command.LeadAssignmentId,
                command.ConsultantProfileId);

            if (lead == null)
                return Result<ExpireLeadNoCallResponse>.Failure("لید یافت نشد");

            var profile = await consultantProfileRepository.GetByIdAsync(command.ConsultantProfileId);
            if (profile == null)
                return Result<ExpireLeadNoCallResponse>.Failure("مشاوری یافت نشد");

            if (profile.IsDeleted)
                return Result<ExpireLeadNoCallResponse>.Failure("پروفایل مشاور حذف شده است");

            if (!profile.IsCompleteProfile)
                return Result<ExpireLeadNoCallResponse>.Failure("پروفایل مشاور کامل نیست");

            if (lead.ReportSubmittedAt.HasValue)
                return Result<ExpireLeadNoCallResponse>.Failure("برای این لید قبلا گزارش ثبت شده است");

            if (lead.CallInitiatedAt.HasValue)
                return Result<ExpireLeadNoCallResponse>.Failure("مشاور تماس را آغاز کرده و امکان منقضی شدن وجود ندارد");

            if (lead.LeadAssignmentState == LeadAssignmentState.Expired)
                return Result<ExpireLeadNoCallResponse>.Failure("این لید قبلا منقضی شده است");

            if (lead.AssignmentType != LeadAssignmentType.RealTime || !lead.RequiresThreeMinuteCall)
                return Result<ExpireLeadNoCallResponse>.Failure("این لید مشمول مهلت سه دقیقه‌ای نیست");

            var now = DateTime.Now;
            if (lead.CallDeadlineAt.HasValue && lead.CallDeadlineAt.Value > now)
                return Result<ExpireLeadNoCallResponse>.Failure("مهلت سه دقیقه‌ای این لید هنوز تمام نشده است");

            lead.LeadAssignmentState = LeadAssignmentState.Expired;
            profile.CurrentScore += NoCallPenaltyScore;

            var hasPendingOfflineLeads = await leadAssignmentRepository.HasPendingOfflineLeadsAsync(profile.Id);
            if (hasPendingOfflineLeads || !leadDomainService.IsWorkingTime(now))
            {
                profile.IsOnline = false;
                profile.LastOfflineAt = now;
            }
            else
            {
                profile.IsOnline = true;
                profile.LastOnlineAt = now;
            }

            await scoreLogRepository.AddAsync(new Domain.Models.ScoreLog
            {
                ConsultantProfileId = profile.Id,
                Source = ScoreSource.System,
                Reason = ScoreReason.LateCall,
                ScoreValue = NoCallPenaltyScore,
                Description = "عدم تماس در بازه سه دقیقه‌ای",
                LeadAssignmentId = lead.Id,
                UserId = profile.UserId,
                CreatedAt = now,
                IsDeleted = false
            });

            leadAssignmentRepository.Update(lead);
            consultantProfileRepository.Update(profile);
            await leadAssignmentRepository.SaveChange();

            if (profile.IsOnline)
                await leadAssignmentService.AssignRealTimeLeadsAsync();

            var response = new ExpireLeadNoCallResponse
            {
                LeadAssignmentId = lead.Id,
                ConsultantProfileId = profile.Id,
                LeadAssignmentState = lead.LeadAssignmentState,
                DeductedScore = NoCallPenaltyScore,
                CurrentScore = profile.CurrentScore,
                IsConsultantOnline = profile.IsOnline
            };

            return Result<ExpireLeadNoCallResponse>.Success(response, "لید منقضی شد و امتیاز مشاور کسر شد");
        }
    }
}
