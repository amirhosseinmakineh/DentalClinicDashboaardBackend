using DentalDashboard.ApplicationService.Contract.Requests.Consultant.Commands;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.Consultant
{
    public class SetOnlineOfflineCommandHandler : ICommandHandler<SetOnlineOfflineCommand>
    {
        private readonly IConsultantProfileRepository consultantProfileRepository;
        private readonly ILeadAssignmentRepository leadAssignmentRepository;

        public SetOnlineOfflineCommandHandler(
            IConsultantProfileRepository consultantProfileRepository,
            ILeadAssignmentRepository leadAssignmentRepository)
        {
            this.consultantProfileRepository = consultantProfileRepository;
            this.leadAssignmentRepository = leadAssignmentRepository;
        }

        public async Task<Result> HandleAsync(
            SetOnlineOfflineCommand command,
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

            if (!profile.IsAvailable)
                return Result.Failure("ابتدا حضور خود را ثبت کنید");

            if (command.IsOnline)
            {
                var hasPendingOfflineLeads =
                    await leadAssignmentRepository.HasPendingOfflineLeadsAsync(profile.Id);

                if (hasPendingOfflineLeads)
                    return Result.Failure("ابتدا لیدهای آفلاین خود را تعیین تکلیف کنید");

                profile.IsOnline = true;
                profile.LastOnlineAt = DateTime.Now;

                consultantProfileRepository.Update(profile);
                await consultantProfileRepository.SaveChange();

                return Result.Success("شما آنلاین شدید");
            }

            profile.IsOnline = false;
            profile.LastOfflineAt = DateTime.Now;

            consultantProfileRepository.Update(profile);
            await consultantProfileRepository.SaveChange();

            return Result.Success("شما آفلاین شدید");
        }
    }
}