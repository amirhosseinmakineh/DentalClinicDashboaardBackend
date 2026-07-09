using DentalDashboard.ApplicationService.Contract.Requests.Consultant.Commands;
using DentalDashboard.ApplicationService.Services;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.Consultant
{
    public class RegisterPushTokenCommandHandler : ICommandHandler<RegisterPushTokenCommand>
    {
        private readonly IConsultantProfileRepository consultantProfileRepository;
        private readonly IPushSubscriptionRepository pushSubscriptionRepository;

        public RegisterPushTokenCommandHandler(
            IConsultantProfileRepository consultantProfileRepository,
            IPushSubscriptionRepository pushSubscriptionRepository)
        {
            this.consultantProfileRepository = consultantProfileRepository;
            this.pushSubscriptionRepository = pushSubscriptionRepository;
        }

        public async Task<Result> HandleAsync(
            RegisterPushTokenCommand command,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(command.DeviceToken))
                return Result.Failure("توکن نوتیفیکیشن معتبر نیست");

            if (!PushSubscriptionJsonParser.TryParse(
                    command.DeviceToken.Trim(),
                    out var endpoint,
                    out var p256dh,
                    out var auth))
            {
                return Result.Failure("فرمت subscription معتبر نیست");
            }

            var profile = await consultantProfileRepository.GetAll()
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.Id == command.ProfileId, cancellationToken);

            if (profile == null || profile.IsDeleted)
                return Result.Failure("مشاوری یافت نشد");

            await pushSubscriptionRepository.UpsertAsync(
                profile.UserId,
                endpoint,
                p256dh,
                auth,
                cancellationToken);

            profile.User.PushNotificationToken = PushSubscriptionStorage.UpsertSubscription(
                profile.User.PushNotificationToken,
                command.DeviceToken.Trim());
            profile.User.UpdatedAt = DateTime.UtcNow;
            await consultantProfileRepository.SaveChange();

            return Result.Success("توکن نوتیفیکیشن ثبت شد");
        }
    }
}
