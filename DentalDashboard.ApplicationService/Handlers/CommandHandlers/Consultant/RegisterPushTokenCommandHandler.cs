using DentalDashboard.ApplicationService.Services;
using DentalDashboard.ApplicationService.Contract.Requests.Consultant.Commands;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.Consultant
{
    public class RegisterPushTokenCommandHandler : ICommandHandler<RegisterPushTokenCommand>
    {
        private readonly IConsultantProfileRepository consultantProfileRepository;

        public RegisterPushTokenCommandHandler(IConsultantProfileRepository consultantProfileRepository)
        {
            this.consultantProfileRepository = consultantProfileRepository;
        }

        public async Task<Result> HandleAsync(RegisterPushTokenCommand command, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(command.DeviceToken))
                return Result.Failure("توکن نوتیفیکیشن معتبر نیست");

            var profile = await consultantProfileRepository.GetAll()
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.Id == command.ProfileId, cancellationToken);

            if (profile == null || profile.IsDeleted)
                return Result.Failure("مشاوری یافت نشد");

            profile.User.PushNotificationToken = PushSubscriptionStorage.UpsertSubscription(
                profile.User.PushNotificationToken,
                command.DeviceToken.Trim());
            await consultantProfileRepository.SaveChange();

            return Result.Success("توکن نوتیفیکیشن ثبت شد");
        }
    }
}
