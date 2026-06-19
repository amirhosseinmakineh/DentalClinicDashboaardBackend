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

        public async Task<Result> HandleAsync(SetAvailableCommand command,CancellationToken cancellationToken = default)
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
                profile.WorkStartTime = DateTime.Now.TimeOfDay;

                consultantProfileRepository.Update(profile);
                await consultantProfileRepository.SaveChange();

                // Pending night/offline leads are also assigned by the background interval;
                // this immediate trigger starts the 5-lead offline batches as soon as attendance is registered.
                await leadAssignmentService.AssignPendingOfflineLeadsAsync();

                return Result.Success("حضور شما ثبت شد");
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