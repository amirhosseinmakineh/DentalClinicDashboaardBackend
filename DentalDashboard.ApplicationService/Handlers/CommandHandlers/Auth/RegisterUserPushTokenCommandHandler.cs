using DentalDashboard.ApplicationService.Contract.Requests.Auth;
using DentalDashboard.ApplicationService.Services;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.Auth;

public class RegisterUserPushTokenCommandHandler : ICommandHandler<RegisterUserPushTokenCommand>
{
    private readonly IUserRepository userRepository;

    public RegisterUserPushTokenCommandHandler(IUserRepository userRepository)
    {
        this.userRepository = userRepository;
    }

    public async Task<Result> HandleAsync(
        RegisterUserPushTokenCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.DeviceToken))
            return Result.Failure("توکن نوتیفیکیشن معتبر نیست");

        var user = await userRepository.GetByIdAsync(command.UserId);
        if (user is null || user.IsDeleted)
            return Result.Failure("کاربر یافت نشد");

        user.PushNotificationToken = PushSubscriptionStorage.UpsertSubscription(
            user.PushNotificationToken,
            command.DeviceToken.Trim());
        user.UpdatedAt = DateTime.UtcNow;
        userRepository.Update(user);
        await userRepository.SaveChange();

        return Result.Success("توکن نوتیفیکیشن ثبت شد");
    }
}
