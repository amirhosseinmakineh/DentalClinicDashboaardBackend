using DentalDashboard.ApplicationService.Contract.Requests.Consultant.Commands;
using DentalDashboard.ApplicationService.Contract.Responses.LeadResponse;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.Consultant
{
    public class RecordLeadCallInitiatedCommandHandler : ICommandHandler<RecordLeadCallInitiatedCommand, RecordLeadCallInitiatedResponse>
    {
        private readonly ILeadAssignmentRepository leadAssignmentRepository;
        private readonly IConsultantProfileRepository consultantProfileRepository;
        private readonly IUnitOfWork unitOfWork;

        public RecordLeadCallInitiatedCommandHandler(
            ILeadAssignmentRepository leadAssignmentRepository,
            IConsultantProfileRepository consultantProfileRepository,
            IUnitOfWork unitOfWork)
        {
            this.leadAssignmentRepository = leadAssignmentRepository;
            this.consultantProfileRepository = consultantProfileRepository;
            this.unitOfWork = unitOfWork;
        }

        public async Task<Result<RecordLeadCallInitiatedResponse>> HandleAsync(
            RecordLeadCallInitiatedCommand command,
            CancellationToken cancellationToken = default)
        {
            var lead = await leadAssignmentRepository.GetByIdAndConsultantAsync(
                command.LeadAssignmentId,
                command.ConsultantProfileId);

            if (lead == null)
                return Result<RecordLeadCallInitiatedResponse>.Failure("لید یافت نشد");

            var profile = await consultantProfileRepository.GetByIdAsync(command.ConsultantProfileId);
            if (profile == null)
                return Result<RecordLeadCallInitiatedResponse>.Failure("مشاوری یافت نشد");

            if (profile.IsDeleted)
                return Result<RecordLeadCallInitiatedResponse>.Failure("پروفایل مشاور حذف شده است");

            if (!profile.IsCompleteProfile)
                return Result<RecordLeadCallInitiatedResponse>.Failure("پروفایل مشاور کامل نیست");

            if (lead.LeadAssignmentState == LeadAssignmentState.Expired)
                return Result<RecordLeadCallInitiatedResponse>.Failure("مهلت تماس این لید به پایان رسیده است");

            if (lead.ReportSubmittedAt.HasValue)
                return Result<RecordLeadCallInitiatedResponse>.Failure("برای این لید قبلا گزارش ثبت شده است");

            var now = DateTime.Now;
            var isFirstCallInitiation = !lead.CallInitiatedAt.HasValue;
            if (isFirstCallInitiation)
            {
                await unitOfWork.BeginTransactionAsync(cancellationToken);
                try
                {
                    lead.CallInitiatedAt = now;
                    leadAssignmentRepository.Update(lead);
                    await unitOfWork.SaveChangesAsync(cancellationToken);

                    if (lead.AssignmentType == LeadAssignmentType.RealTime)
                    {
                        profile.IsOnline = false;
                        profile.LastOfflineAt = now;
                        consultantProfileRepository.Update(profile);
                        await unitOfWork.SaveChangesAsync(cancellationToken);
                    }

                    await unitOfWork.CommitAsync(cancellationToken);
                }
                catch
                {
                    await unitOfWork.RollbackAsync(CancellationToken.None);
                    throw;
                }
            }

            return Result<RecordLeadCallInitiatedResponse>.Success(new RecordLeadCallInitiatedResponse
            {
                LeadAssignmentId = lead.Id,
                ConsultantProfileId = profile.Id,
                CallInitiatedAt = lead.CallInitiatedAt ?? now
            }, "شروع تماس ثبت شد");
        }
    }
}
