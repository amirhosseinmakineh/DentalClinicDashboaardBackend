using DentalDashboard.ApplicationService.Contract.Requests.Consultant.Commands;
using DentalDashboard.ApplicationService.Contract.Responses.LeadResponse;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Models;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.Consultant
{
    public class AddConsultantPatientLeadCommandHandler : ICommandHandler<AddConsultantPatientLeadCommand, AddConsultantPatientLeadResponse>
    {
        private static readonly Regex IranianMobileRegex = new(@"^09\d{9}$", RegexOptions.Compiled);

        private readonly ILeadAssignmentRepository leadAssignmentRepository;
        private readonly IConsultantProfileRepository consultantProfileRepository;

        public AddConsultantPatientLeadCommandHandler(
            ILeadAssignmentRepository leadAssignmentRepository,
            IConsultantProfileRepository consultantProfileRepository)
        {
            this.leadAssignmentRepository = leadAssignmentRepository;
            this.consultantProfileRepository = consultantProfileRepository;
        }

        public async Task<Result<AddConsultantPatientLeadResponse>> HandleAsync(
            AddConsultantPatientLeadCommand command,
            CancellationToken cancellationToken = default)
        {
            var userName = command.UserName?.Trim() ?? string.Empty;
            var phoneNumber = command.PhoneNumber?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(userName))
                return Result<AddConsultantPatientLeadResponse>.Failure("نام بیمار الزامی است");

            if (!IranianMobileRegex.IsMatch(phoneNumber))
                return Result<AddConsultantPatientLeadResponse>.Failure("شماره موبایل باید با 09 شروع شود و ۱۱ رقم باشد");

            var profile = await consultantProfileRepository.GetByIdAsync(command.ConsultantProfileId);
            if (profile == null)
                return Result<AddConsultantPatientLeadResponse>.Failure("مشاوری یافت نشد");

            if (profile.IsDeleted)
                return Result<AddConsultantPatientLeadResponse>.Failure("پروفایل مشاور حذف شده است");

            if (!profile.IsCompleteProfile)
                return Result<AddConsultantPatientLeadResponse>.Failure("پروفایل مشاور کامل نیست");

            var activeLead = await leadAssignmentRepository.GetAll()
                .FirstOrDefaultAsync(x => !x.IsDeleted &&
                                          x.ConsultantProfileId == command.ConsultantProfileId &&
                                          x.PhoneNumber == phoneNumber &&
                                          x.ReportSubmittedAt == null &&
                                          x.LeadAssignmentState != LeadAssignmentState.Expired &&
                                          x.LeadAssignmentState != LeadAssignmentState.Rejected,
                    cancellationToken);

            // A burned lead becomes active again when it is dispatched to a consultant.
            // If the consultant subsequently adds that same person as their own patient,
            // reuse the dispatched row instead of rejecting the request or creating a
            // second lead for the same phone number.
            if (activeLead?.AssignmentType == LeadAssignmentType.RealTime)
            {
                var reactivatedAt = DateTime.Now;
                activeLead.UserName = userName;
                activeLead.AssignmentType = LeadAssignmentType.ConsultantPatient;
                activeLead.LeadAssignmentState = LeadAssignmentState.Assigned;
                activeLead.AssignedAt ??= reactivatedAt;
                activeLead.RequiresThreeMinuteCall = false;
                activeLead.CallDeadlineAt = null;
                activeLead.PatientCity = command.PatientCity?.Trim();
                activeLead.PatientRegion = command.PatientRegion?.Trim();
                activeLead.SecondaryPhoneNumber = command.SecondaryPhoneNumber?.Trim();
                activeLead.ReportDescription = command.ReportDescription?.Trim();
                activeLead.UpdatedAt = reactivatedAt;

                leadAssignmentRepository.Update(activeLead);
                await leadAssignmentRepository.SaveChange();

                return Result<AddConsultantPatientLeadResponse>.Success(new AddConsultantPatientLeadResponse
                {
                    LeadAssignmentId = activeLead.Id,
                    ConsultantProfileId = profile.Id,
                    UserName = activeLead.UserName,
                    PhoneNumber = activeLead.PhoneNumber,
                    AssignmentType = activeLead.AssignmentType,
                    LeadAssignmentState = activeLead.LeadAssignmentState
                }, "بیمار با موفقیت ثبت شد");
            }

            if (activeLead != null)
                return Result<AddConsultantPatientLeadResponse>.Failure(
                    "برای این شماره تماس، لید فعال دیگری نزد شما وجود دارد");

            var now = DateTime.Now;
            var lead = new LeadAssignment
            {
                UserName = userName,
                PhoneNumber = phoneNumber,
                ConsultantProfileId = profile.Id,
                LeadAssignmentState = LeadAssignmentState.Assigned,
                AssignmentType = LeadAssignmentType.ConsultantPatient,
                AssignedAt = now,
                CreatedAt = now,
                RequiresThreeMinuteCall = false,
                NotificationSent = true,
                SmsSent = false,
                PatientCity = command.PatientCity?.Trim(),
                PatientRegion = command.PatientRegion?.Trim(),
                SecondaryPhoneNumber = command.SecondaryPhoneNumber?.Trim(),
                ReportDescription = command.ReportDescription?.Trim(),
                IsDeleted = false
            };

            await leadAssignmentRepository.AddAsync(lead);
            await leadAssignmentRepository.SaveChange();

            return Result<AddConsultantPatientLeadResponse>.Success(new AddConsultantPatientLeadResponse
            {
                LeadAssignmentId = lead.Id,
                ConsultantProfileId = profile.Id,
                UserName = lead.UserName,
                PhoneNumber = lead.PhoneNumber,
                AssignmentType = lead.AssignmentType,
                LeadAssignmentState = lead.LeadAssignmentState
            }, "بیمار با موفقیت ثبت شد");
        }
    }
}
