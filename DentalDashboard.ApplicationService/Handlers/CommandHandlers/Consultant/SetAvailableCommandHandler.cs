using DentalDashboard.ApplicationService.Contract.Requests.Consultant.Commands;
using DentalDashboard.Domain.IDomainService;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.Consultant
{
    public class SetAvailableCommandHandler : ICommandHandler<SetAvailableCommand>
    {
        private readonly IConsultantProfileRepository consultantProfileRepository;
        private readonly ILeadDomainService leadDomainService;

        public SetAvailableCommandHandler(
            IConsultantProfileRepository consultantProfileRepository,
            ILeadDomainService leadDomainService)
        {
            this.consultantProfileRepository = consultantProfileRepository;
            this.leadDomainService = leadDomainService;
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
                if (!leadDomainService.IsWorkingTime(DateTime.Now))
                    return Result.Failure("امکان ثبت حضور فقط بین ساعت ۹ صبح تا ۹ شب وجود دارد");

                profile.IsAvailable = true;
                profile.WorkStartTime = DateTime.Now.TimeOfDay;

                consultantProfileRepository.Update(profile);
                await consultantProfileRepository.SaveChange();

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