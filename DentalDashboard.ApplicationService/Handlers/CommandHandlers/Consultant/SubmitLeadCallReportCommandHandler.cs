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

        public SubmitLeadCallReportCommandHandler(
            ILeadAssignmentRepository leadAssignmentRepository,
            IConsultantProfileRepository consultantProfileRepository,
            ILeadReportDomainService leadReportDomainService,
            ILeadDomainService leadDomainService,
            ILeadAssignmentService leadAssignmentService)
        {
            this.leadAssignmentRepository = leadAssignmentRepository;
            this.consultantProfileRepository = consultantProfileRepository;
            this.leadReportDomainService = leadReportDomainService;
            this.leadDomainService = leadDomainService;
            this.leadAssignmentService = leadAssignmentService;
        }

        public async Task<Result<SubmitLeadCallReportResponse>> HandleAsync(SubmitLeadCallReportCommand command, CancellationToken cancellationToken = default)
        {
            var lead = await leadAssignmentRepository.GetByIdAndConsultantAsync(command.LeadAssignmentId, command.ConsultantProfileId);
            if (lead == null)
                return Result<SubmitLeadCallReportResponse>.Failure("لید یافت نشد");

            var profile = await consultantProfileRepository.GetAll()
                .FirstOrDefaultAsync(x => x.Id == command.ConsultantProfileId);
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

            if (lead.AssignmentType == LeadAssignmentType.ConsultantPatient)
            {
                consultantProfileRepository.Update(profile);
                leadAssignmentRepository.Update(lead);
                await leadAssignmentRepository.SaveChange();
                return Result<SubmitLeadCallReportResponse>.Success(CreateResponse(lead, profile), "گزارش ثبت شد");
            }

            if (!leadDomainService.IsWorkingTime(now))
            {
                profile.IsOnline = false;
                profile.LastOfflineAt = now;
                consultantProfileRepository.Update(profile);
                leadAssignmentRepository.Update(lead);
                await leadAssignmentRepository.SaveChange();
                return Result<SubmitLeadCallReportResponse>.Success(CreateResponse(lead, profile), "گزارش ثبت شد");
            }

            profile.IsOnline = true;
            profile.LastOnlineAt = now;
            consultantProfileRepository.Update(profile);
            leadAssignmentRepository.Update(lead);
            await leadAssignmentRepository.SaveChange();

            await leadAssignmentService.AssignRealTimeLeadsAsync();

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
