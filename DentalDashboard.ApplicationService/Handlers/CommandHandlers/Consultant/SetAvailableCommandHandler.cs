using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.ApplicationService.Contract.Requests.Consultant.Commands;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.Consultant
{
    public class SetAvailableCommandHandler : ICommandHandler<SetAvailableCommand>
    {
        private readonly IConsultantProfileRepository consultantProfileRepository;
        private readonly ILeadAssignmentService leadAssignmentService;

        public SetAvailableCommandHandler(
            IConsultantProfileRepository consultantProfileRepository,
            ILeadAssignmentService leadAssignmentService)
        {
            this.consultantProfileRepository = consultantProfileRepository;
            this.leadAssignmentService = leadAssignmentService;
        }

        public async Task<Result> HandleAsync(
            SetAvailableCommand command,
            CancellationToken cancellationToken = default)
        {
            var profile = await consultantProfileRepository.GetAll()
                .FirstOrDefaultAsync(x => x.Id == command.ProfileId, cancellationToken);

            if (profile == null)
                return Result.Failure("مشاوری یافت نشد");

            if (profile.IsDeleted)
                return Result.Failure("پروفایل مشاور حذف شده است");

            if (!profile.IsCompleteProfile)
                return Result.Failure("پروفایل مشاور کامل نیست");

            if (command.IsAvailable)
            {
                profile.IsAvailable = true;
                profile.IsOnline = false;
                profile.WorkStartTime = DateTime.Now.TimeOfDay;

                consultantProfileRepository.Update(profile);
                await consultantProfileRepository.SaveChange();

                var assignedCount =
                    await leadAssignmentService.AssignPendingOfflineLeadsForConsultantAsync(profile.Id);

                return Result.Success(assignedCount > 0
                    ? $"حضور شما ثبت شد؛ {assignedCount} لید آفلاین دریافت کردید"
                    : "حضور شما ثبت شد؛ در صف آفلاین لیدی برای تخصیص نبود");
            }

            profile.IsAvailable = false;
            profile.IsOnline = false;
            profile.WorkEndTime = DateTime.Now.TimeOfDay;
            profile.LastOfflineAt = DateTime.Now;

            consultantProfileRepository.Update(profile);
            await consultantProfileRepository.SaveChange();

            return Result.Success("عدم حضور شما ثبت شد");
        }
    }
}
