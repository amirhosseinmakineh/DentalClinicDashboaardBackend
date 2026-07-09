using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.ApplicationService.Contract.Requests.Consultant.Commands;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IDomainService;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.Consultant
{
    public class SetOnlineOfflineCommandHandler : ICommandHandler<SetOnlineOfflineCommand>
    {
        private readonly IConsultantProfileRepository consultantProfileRepository;
        private readonly ILeadAssignmentService leadAssignmentService;
        private readonly ILeadDomainService leadDomainService;
        private readonly IUserPresenceService presenceService;

        public SetOnlineOfflineCommandHandler(
            IConsultantProfileRepository consultantProfileRepository,
            ILeadAssignmentService leadAssignmentService,
            ILeadDomainService leadDomainService,
            IUserPresenceService presenceService)
        {
            this.consultantProfileRepository = consultantProfileRepository;
            this.leadAssignmentService = leadAssignmentService;
            this.leadDomainService = leadDomainService;
            this.presenceService = presenceService;
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
                if (!leadDomainService.IsWorkingTime(DateTime.Now))
                    return Result.Failure("امکان آنلاین شدن فقط بین ساعت ۹ صبح تا ۹ شب وجود دارد");

                profile.IsOnline = true;
                profile.LastOnlineAt = DateTime.Now;

                consultantProfileRepository.Update(profile);
                await consultantProfileRepository.SaveChange();

                await presenceService.LogAsync(
                    profile.UserId,
                    UserPresenceEventType.Online,
                    profile.LastOnlineAt,
                    cancellationToken: cancellationToken);

                await leadAssignmentService.AssignRealTimeLeadsAsync();

                return Result.Success("شما آنلاین شدید");
            }

            profile.IsOnline = false;
            profile.LastOfflineAt = DateTime.Now;

            consultantProfileRepository.Update(profile);
            await consultantProfileRepository.SaveChange();

            await presenceService.LogAsync(
                profile.UserId,
                UserPresenceEventType.Offline,
                profile.LastOfflineAt,
                cancellationToken: cancellationToken);

            return Result.Success("شما آفلاین شدید");
        }
    }
}
