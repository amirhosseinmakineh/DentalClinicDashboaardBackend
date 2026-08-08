using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.ApplicationService.Contract.Requests.Consultant.Commands;
using DentalDashboard.ApplicationService.Contract.Responses.LeadResponse;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IDomainService;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Models;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.Consultant
{
    public class SubmitLeadCallReportCommandHandler : ICommandHandler<SubmitLeadCallReportCommand, SubmitLeadCallReportResponse>
    {
        private readonly ILeadAssignmentRepository leadAssignmentRepository;
        private readonly IConsultantProfileRepository consultantProfileRepository;
        private readonly ILeadReportDomainService leadReportDomainService;
        private readonly ILeadDomainService leadDomainService;
        private readonly ILeadAssignmentService leadAssignmentService;
        private readonly IUserPresenceService presenceService;
        private readonly IUnitOfWork unitOfWork;

        public SubmitLeadCallReportCommandHandler(
            ILeadAssignmentRepository leadAssignmentRepository,
            IConsultantProfileRepository consultantProfileRepository,
            ILeadReportDomainService leadReportDomainService,
            ILeadDomainService leadDomainService,
            ILeadAssignmentService leadAssignmentService,
            IUserPresenceService presenceService,
            IUnitOfWork unitOfWork)
        {
            this.leadAssignmentRepository = leadAssignmentRepository;
            this.consultantProfileRepository = consultantProfileRepository;
            this.leadReportDomainService = leadReportDomainService;
            this.leadDomainService = leadDomainService;
            this.leadAssignmentService = leadAssignmentService;
            this.presenceService = presenceService;
            this.unitOfWork = unitOfWork;
        }

        public async Task<Result<SubmitLeadCallReportResponse>> HandleAsync(SubmitLeadCallReportCommand command, CancellationToken cancellationToken = default)
        {
            var lead = await leadAssignmentRepository.GetAll()
                .FirstOrDefaultAsync(
                    x => x.Id == command.LeadAssignmentId &&
                         x.ConsultantProfileId == command.ConsultantProfileId,
                    cancellationToken);
            if (lead == null)
                return Result<SubmitLeadCallReportResponse>.Failure("لید یافت نشد");

            var profile = await consultantProfileRepository.GetAll()
                .FirstOrDefaultAsync(x => x.Id == command.ConsultantProfileId, cancellationToken);
            if (profile == null)
                return Result<SubmitLeadCallReportResponse>.Failure("مشاوری یافت نشد");

            if (profile.IsDeleted)
                return Result<SubmitLeadCallReportResponse>.Failure("پروفایل مشاور حذف شده است");

            if (!profile.IsCompleteProfile)
                return Result<SubmitLeadCallReportResponse>.Failure("پروفایل مشاور کامل نیست");

            if (lead.LeadAssignmentState == LeadAssignmentState.Expired)
                return Result<SubmitLeadCallReportResponse>.Failure("مهلت ثبت گزارش این لید به پایان رسیده است");

            if (lead.ReportSubmittedAt.HasValue)
                return Result<SubmitLeadCallReportResponse>.Failure("گزارش این لید قبلا ثبت شده است");

            if (command.AttendanceProbabilityPercent.HasValue && (command.AttendanceProbabilityPercent < 0 || command.AttendanceProbabilityPercent > 100))
                return Result<SubmitLeadCallReportResponse>.Failure("احتمال حضور باید بین ۰ تا ۱۰۰ باشد");

            var isSuccessfulCall = command.CallResult == LeadCallResult.Contacted ||
                                   command.CallResult == LeadCallResult.Converted;

            if (isSuccessfulCall)
            {
                if (string.IsNullOrWhiteSpace(command.PatientCity))
                    return Result<SubmitLeadCallReportResponse>.Failure("شهر بیمار الزامی است");

                if (string.IsNullOrWhiteSpace(command.PatientRegion))
                    return Result<SubmitLeadCallReportResponse>.Failure("منطقه بیمار الزامی است");
            }
            else if (string.IsNullOrWhiteSpace(command.ReportDescription))
            {
                return Result<SubmitLeadCallReportResponse>.Failure("توضیحات گزارش الزامی است");
            }

            var now = DateTime.Now;
            lead.CallResult = command.CallResult;
            lead.ReportDescription = command.ReportDescription;
            lead.PatientCity = command.PatientCity?.Trim();
            lead.PatientRegion = command.PatientRegion?.Trim();
            lead.AttendanceProbabilityPercent = command.AttendanceProbabilityPercent;
            lead.SecondaryPhoneNumber = command.SecondaryPhoneNumber?.Trim();
            lead.ReportSubmittedAt = now;
            lead.ContactedAt = now;
            lead.LeadAssignmentState = leadReportDomainService.MapCallResultToState(command.CallResult);

            // Persist the lead before touching ConsultantProfiles. Pickup uses
            // the same LeadAssignments -> ConsultantProfiles lock order. This
            // prevents a pickup transaction holding the consultant row while
            // waiting for a lead row, which was the blocker observed in SQL
            // Server (LCK_M_X behind the pickup session).
            await unitOfWork.SaveChangesAsync(cancellationToken);

            if (lead.AssignmentType == LeadAssignmentType.ConsultantPatient)
            {
                return Result<SubmitLeadCallReportResponse>.Success(CreateResponse(lead, profile), "گزارش ثبت شد");
            }

            if (!leadDomainService.IsWorkingTime(now))
            {
                profile.IsOnline = false;
                profile.LastOfflineAt = now;
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return Result<SubmitLeadCallReportResponse>.Success(CreateResponse(lead, profile), "گزارش ثبت شد");
            }

            var wasOnline = profile.IsOnline;
            if (wasOnline)
            {
                profile.IsOnline = true;
                profile.LastOnlineAt = now;
                await unitOfWork.SaveChangesAsync(cancellationToken);

                await presenceService.LogAsync(
                    profile.UserId,
                    UserPresenceEventType.Online,
                    profile.LastOnlineAt,
                    cancellationToken: cancellationToken);

                await leadAssignmentService.AssignRealTimeLeadsAsync();
            }

            return Result<SubmitLeadCallReportResponse>.Success(CreateResponse(lead, profile), "گزارش ثبت شد");
        }

        private static SubmitLeadCallReportResponse CreateResponse(LeadAssignment lead, ConsultantProfile profile)
        {
            return new SubmitLeadCallReportResponse
            {
                LeadAssignmentId = lead.Id,
                ConsultantProfileId = profile.Id,
                IsReportSubmitted = lead.ReportSubmittedAt.HasValue,
                ReportSubmittedAt = lead.ReportSubmittedAt ?? DateTime.Now,
                LeadAssignmentState = lead.LeadAssignmentState,
                CallResult = lead.CallResult!.Value,
                IsConsultantOnline = profile.IsOnline,
                ShouldOpenReservationPage = lead.CallResult == LeadCallResult.Contacted || lead.CallResult == LeadCallResult.Converted
            };
        }
    }
}
