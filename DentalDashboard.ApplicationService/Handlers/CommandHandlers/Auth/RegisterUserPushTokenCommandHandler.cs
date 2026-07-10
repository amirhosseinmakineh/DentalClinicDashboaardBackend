using DentalDashboard.ApplicationService.Contract.Requests.Auth;
using DentalDashboard.ApplicationService.Services;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.Auth;

public class RegisterUserPushTokenCommandHandler : ICommandHandler<RegisterUserPushTokenCommand>
{
    private readonly IUserRepository userRepository;
    private readonly IPushSubscriptionRepository pushSubscriptionRepository;

    public RegisterUserPushTokenCommandHandler(
        IUserRepository userRepository,
        IPushSubscriptionRepository pushSubscriptionRepository)
    {
        this.userRepository = userRepository;
        this.pushSubscriptionRepository = pushSubscriptionRepository;
    }

    public async Task<Result> HandleAsync(
        RegisterUserPushTokenCommand command,
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

        try
        {
            var user = await userRepository.GetByIdAsync(command.UserId);
            if (user is null || user.IsDeleted)
                return Result.Failure("کاربر یافت نشد");

            await pushSubscriptionRepository.UpsertAsync(
                user.Id,
                endpoint,
                p256dh,
                auth,
                cancellationToken);

            user.PushNotificationToken = PushSubscriptionStorage.UpsertSubscription(
                user.PushNotificationToken,
                command.DeviceToken.Trim());
            user.UpdatedAt = DateTime.UtcNow;
            userRepository.Update(user);
            await pushSubscriptionRepository.SaveChange();

            return Result.Success("توکن نوتیفیکیشن ثبت شد");
        }
        catch (DbUpdateException ex)
        {
            var inner = ex.InnerException?.Message ?? ex.Message;
            return Result.Failure($"ثبت subscription در دیتابیس انجام نشد: {inner}");
        }
        catch (Exception ex)
        {
            return Result.Failure($"ثبت subscription انجام نشد: {ex.Message}");
        }
    }
}
