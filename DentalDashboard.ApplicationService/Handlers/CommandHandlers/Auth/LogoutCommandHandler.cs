using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.ApplicationService.Contract.Requests.Auth;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.Auth;

public class LogoutCommandHandler : ICommandHandler<LogoutCommand>
{
    private readonly IUserPresenceService presenceService;

    public LogoutCommandHandler(IUserPresenceService presenceService)
    {
        this.presenceService = presenceService;
    }

    public async Task<Result> HandleAsync(
        LogoutCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.UserId == Guid.Empty)
            return Result.Failure("شناسه کاربر معتبر نیست");

        await presenceService.LogAsync(
            command.UserId,
            UserPresenceEventType.Logout,
            cancellationToken: cancellationToken);

        return Result.Success("خروج با موفقیت ثبت شد");
    }
}
