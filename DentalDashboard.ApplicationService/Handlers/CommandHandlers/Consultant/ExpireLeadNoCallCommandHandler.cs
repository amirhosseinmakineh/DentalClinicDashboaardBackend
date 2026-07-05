using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.ApplicationService.Contract.Requests.Consultant.Commands;
using DentalDashboard.ApplicationService.Contract.Responses.LeadResponse;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.Consultant
{
    public class ExpireLeadNoCallCommandHandler : ICommandHandler<ExpireLeadNoCallCommand, ExpireLeadNoCallResponse>
    {
        private readonly ILeadAssignmentRepository leadAssignmentRepository;
        private readonly IConsultantProfileRepository consultantProfileRepository;
        private readonly ILeadAssignmentService leadAssignmentService;

        public ExpireLeadNoCallCommandHandler(
            ILeadAssignmentRepository leadAssignmentRepository,
            IConsultantProfileRepository consultantProfileRepository,
            ILeadAssignmentService leadAssignmentService)
        {
            this.leadAssignmentRepository = leadAssignmentRepository;
            this.consultantProfileRepository = consultantProfileRepository;
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

            var profile = await consultantProfileRepository.GetAll()
                .Include(x => x.ScoreLogs)
                .FirstOrDefaultAsync(x => x.Id == command.ConsultantProfileId);
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

            if (lead.AssignmentType != LeadAssignmentType.RealTime || !lead.RequiresThreeMinuteCall)
                return Result<ExpireLeadNoCallResponse>.Failure("این لید مشمول مهلت سه دقیقه‌ای نیست");

            var now = DateTime.Now;
            if (lead.CallDeadlineAt.HasValue && lead.CallDeadlineAt.Value > now)
                return Result<ExpireLeadNoCallResponse>.Failure("مهلت سه دقیقه‌ای این لید هنوز تمام نشده است");

            var result = await leadAssignmentService.ExpireAndRequeueRealTimeLeadAsync(lead, profile);

            var response = new ExpireLeadNoCallResponse
            {
                LeadAssignmentId = result.LeadAssignmentId,
                ConsultantProfileId = result.ConsultantProfileId,
                LeadAssignmentState = result.LeadAssignmentState,
                DeductedScore = result.EventScore,
                EventScore = result.EventScore,
                CurrentScore = result.CurrentScore,
                IsConsultantOnline = result.IsConsultantOnline,
                WasRequeued = result.WasRequeued
            };

            return Result<ExpireLeadNoCallResponse>.Success(
                response,
                "مهلت تماس شما تمام شد، امتیاز کسر شد و لید برای مشاور آنلاین دیگر ارسال شد");
        }
    }
}
