using DentalDashboard.ApplicationService.Contract.Requests.Consultant.Commands;
using DentalDashboard.ApplicationService.Contract.Responses.LeadResponse;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IDomainService;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.Consultant
{
    public class UpdateLeadCallReportCommandHandler : ICommandHandler<UpdateLeadCallReportCommand, SubmitLeadCallReportResponse>
    {
        private readonly ILeadAssignmentRepository leadAssignmentRepository;
        private readonly IConsultantProfileRepository consultantProfileRepository;
        private readonly ILeadReportDomainService leadReportDomainService;

        public UpdateLeadCallReportCommandHandler(
            ILeadAssignmentRepository leadAssignmentRepository,
            IConsultantProfileRepository consultantProfileRepository,
            ILeadReportDomainService leadReportDomainService)
        {
            this.leadAssignmentRepository = leadAssignmentRepository;
            this.consultantProfileRepository = consultantProfileRepository;
            this.leadReportDomainService = leadReportDomainService;
        }

        public async Task<Result<SubmitLeadCallReportResponse>> HandleAsync(
            UpdateLeadCallReportCommand command,
            CancellationToken cancellationToken = default)
        {
            var lead = await leadAssignmentRepository.GetByIdAndConsultantAsync(
                command.LeadAssignmentId,
                command.ConsultantProfileId);

            if (lead == null)
                return Result<SubmitLeadCallReportResponse>.Failure("لید یافت نشد");

            var profile = await consultantProfileRepository.GetByIdAsync(command.ConsultantProfileId);
            if (profile == null)
                return Result<SubmitLeadCallReportResponse>.Failure("مشاوری یافت نشد");

            if (profile.IsDeleted)
                return Result<SubmitLeadCallReportResponse>.Failure("پروفایل مشاور حذف شده است");

            if (!profile.IsCompleteProfile)
                return Result<SubmitLeadCallReportResponse>.Failure("پروفایل مشاور کامل نیست");

            if (!lead.ReportSubmittedAt.HasValue)
                return Result<SubmitLeadCallReportResponse>.Failure("گزارشی برای ویرایش یافت نشد");

            if (lead.LeadAssignmentState is LeadAssignmentState.Converted or LeadAssignmentState.Rejected or LeadAssignmentState.Expired)
                return Result<SubmitLeadCallReportResponse>.Failure("ویرایش گزارش این لید امکان‌پذیر نیست");

            if (command.AttendanceProbabilityPercent.HasValue &&
                (command.AttendanceProbabilityPercent < 0 || command.AttendanceProbabilityPercent > 100))
                return Result<SubmitLeadCallReportResponse>.Failure("احتمال حضور باید بین ۰ تا ۱۰۰ باشد");

            var isSuccessfulCall = command.CallResult is LeadCallResult.Contacted or LeadCallResult.Converted;

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

            lead.CallResult = command.CallResult;
            lead.ReportDescription = command.ReportDescription;
            lead.PatientCity = command.PatientCity?.Trim();
            lead.PatientRegion = command.PatientRegion?.Trim();
            lead.BusinessName = command.BusinessName?.Trim();
            lead.AttendanceProbabilityPercent = command.AttendanceProbabilityPercent;
            lead.SecondaryPhoneNumber = command.SecondaryPhoneNumber?.Trim();
            lead.LeadAssignmentState = leadReportDomainService.MapCallResultToState(command.CallResult);

            leadAssignmentRepository.Update(lead);
            await leadAssignmentRepository.SaveChange();

            return Result<SubmitLeadCallReportResponse>.Success(new SubmitLeadCallReportResponse
            {
                LeadAssignmentId = lead.Id,
                ConsultantProfileId = profile.Id,
                IsReportSubmitted = true,
                ReportSubmittedAt = lead.ReportSubmittedAt ?? DateTime.Now,
                LeadAssignmentState = lead.LeadAssignmentState,
                CallResult = lead.CallResult!.Value,
                IsConsultantOnline = profile.IsOnline,
                ShouldOpenReservationPage = lead.CallResult is LeadCallResult.Contacted or LeadCallResult.Converted
            }, "گزارش ویرایش شد");
        }
    }
}
