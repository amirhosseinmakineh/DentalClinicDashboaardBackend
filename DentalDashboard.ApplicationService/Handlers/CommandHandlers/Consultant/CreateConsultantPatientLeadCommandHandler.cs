using DentalDashboard.ApplicationService.Contract.Requests.Consultant.Commands;
using DentalDashboard.ApplicationService.Contract.Responses.LeadResponse;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Models;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.Consultant
{
    public class CreateConsultantPatientLeadCommandHandler
        : ICommandHandler<CreateConsultantPatientLeadCommand, CreateConsultantPatientLeadResponse>
    {
        private readonly ILeadAssignmentRepository leadAssignmentRepository;
        private readonly IConsultantProfileRepository consultantProfileRepository;

        public CreateConsultantPatientLeadCommandHandler(
            ILeadAssignmentRepository leadAssignmentRepository,
            IConsultantProfileRepository consultantProfileRepository)
        {
            this.leadAssignmentRepository = leadAssignmentRepository;
            this.consultantProfileRepository = consultantProfileRepository;
        }

        public async Task<Result<CreateConsultantPatientLeadResponse>> HandleAsync(
            CreateConsultantPatientLeadCommand command,
            CancellationToken cancellationToken = default)
        {
            var profile = await consultantProfileRepository.GetByIdAsync(command.ConsultantProfileId);
            if (profile == null || profile.IsDeleted)
                return Result<CreateConsultantPatientLeadResponse>.Failure("مشاور یافت نشد");

            if (!profile.IsCompleteProfile)
                return Result<CreateConsultantPatientLeadResponse>.Failure("پروفایل مشاور کامل نیست");

            var phoneNumber = command.PhoneNumber.Trim();
            var hasActiveLead = await leadAssignmentRepository.GetAll()
                .AnyAsync(x => !x.IsDeleted &&
                               x.ConsultantProfileId == command.ConsultantProfileId &&
                               x.PhoneNumber == phoneNumber &&
                               x.ReportSubmittedAt == null &&
                               x.LeadAssignmentState != LeadAssignmentState.Expired &&
                               x.LeadAssignmentState != LeadAssignmentState.Rejected,
                    cancellationToken);

            if (hasActiveLead)
                return Result<CreateConsultantPatientLeadResponse>.Failure(
                    "برای این شماره تماس، لید فعال دیگری نزد شما وجود دارد");

            var now = DateTime.Now;
            var lead = new LeadAssignment
            {
                UserName = command.UserName.Trim(),
                PhoneNumber = phoneNumber,
                ConsultantProfileId = command.ConsultantProfileId,
                AssignmentType = LeadAssignmentType.ConsultantOwned,
                LeadAssignmentState = LeadAssignmentState.Assigned,
                AssignedAt = now,
                CreatedAt = now,
                RequiresThreeMinuteCall = false,
                NotificationSent = true,
                PatientCity = command.PatientCity?.Trim(),
                PatientRegion = command.PatientRegion?.Trim(),
                SecondaryPhoneNumber = command.SecondaryPhoneNumber?.Trim()
            };

            await leadAssignmentRepository.AddAsync(lead);
            await leadAssignmentRepository.SaveChange();

            return Result<CreateConsultantPatientLeadResponse>.Success(
                new CreateConsultantPatientLeadResponse
                {
                    LeadAssignmentId = lead.Id,
                    ConsultantProfileId = profile.Id,
                    UserName = lead.UserName,
                    PhoneNumber = lead.PhoneNumber,
                    LeadAssignmentState = lead.LeadAssignmentState,
                    LeadAssignmentType = lead.AssignmentType,
                    AssignedAt = lead.AssignedAt!.Value
                },
                "مریض با موفقیت ثبت شد");
        }
    }
}
