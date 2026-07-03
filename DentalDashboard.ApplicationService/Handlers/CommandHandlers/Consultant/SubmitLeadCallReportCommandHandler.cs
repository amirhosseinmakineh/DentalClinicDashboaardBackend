using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.ApplicationService.Contract.Requests.Consultant.Commands;
using DentalDashboard.ApplicationService.Contract.Responses.LeadResponse;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IDomainService;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Models;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;

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

            var profile = await consultantProfileRepository.GetByIdAsync(command.ConsultantProfileId);
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
            lead.BusinessName = command.BusinessName?.Trim();
            lead.AttendanceProbabilityPercent = command.AttendanceProbabilityPercent;
            lead.SecondaryPhoneNumber = command.SecondaryPhoneNumber?.Trim();
            lead.ReportSubmittedAt = now;
            lead.ContactedAt = now;
            lead.LeadAssignmentState = leadReportDomainService.MapCallResultToState(command.CallResult);

            var scoreLog = CreateScoreLog(lead, profile, command.CallResult, now);
            profile.CurrentScore += scoreLog.ScoreValue;
            profile.ScoreLogs.Add(scoreLog);

            consultantProfileRepository.Update(profile);
            leadAssignmentRepository.Update(lead);
            await leadAssignmentRepository.SaveChange();

            if (lead.AssignmentType == LeadAssignmentType.ConsultantOwned)
                return Result<SubmitLeadCallReportResponse>.Success(CreateResponse(lead, profile), "گزارش ثبت شد");

            var hasPendingOfflineLeads = await leadAssignmentRepository.HasPendingOfflineLeadsAsync(profile.Id);
            if (hasPendingOfflineLeads)
            {
                profile.IsOnline = false;
                profile.LastOfflineAt = now;
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

        private static DentalDashboard.Domain.Models.ScoreLog CreateScoreLog(
            LeadAssignment lead,
            ConsultantProfile profile,
            LeadCallResult callResult,
            DateTime now)
        {
            var (reason, scoreValue, description) = callResult switch
            {
                LeadCallResult.Contacted => (ScoreReason.SuccessfulCall, 5, "تماس موفق با لید"),
                LeadCallResult.Converted => (ScoreReason.SuccessfulCall, 10, "تبدیل لید پس از تماس"),
                LeadCallResult.NeedFollowUp => (ScoreReason.SuccessfulCall, 3, "تماس نیازمند پیگیری"),
                LeadCallResult.NoAnswer => (ScoreReason.NoAnswer, -2, "عدم پاسخگویی لید"),
                LeadCallResult.Rejected => (ScoreReason.FailedCall, -3, "رد شدن لید پس از تماس"),
                LeadCallResult.WrongNumber => (ScoreReason.FailedCall, -5, "شماره تماس اشتباه"),
                _ => (ScoreReason.FailedCall, 0, "ثبت گزارش تماس لید")
            };

            return new DentalDashboard.Domain.Models.ScoreLog
            {
                ConsultantProfileId = profile.Id,
                Source = ScoreSource.System,
                Reason = reason,
                ScoreValue = scoreValue,
                Description = description,
                LeadAssignmentId = lead.Id,
                UserId = profile.UserId,
                CreatedAt = now,
                IsDeleted = false
            };
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
