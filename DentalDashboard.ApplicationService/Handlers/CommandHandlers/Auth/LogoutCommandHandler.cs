using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.ApplicationService.Contract.Requests.Auth;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.Auth;

public class LogoutCommandHandler : ICommandHandler<LogoutCommand>
{
    private readonly IUserPresenceService presenceService;
    private readonly IConsultantProfileRepository consultantProfileRepository;

    public LogoutCommandHandler(IUserPresenceService presenceService,
        IConsultantProfileRepository consultantProfileRepository)
    {
        this.presenceService = presenceService;
        this.consultantProfileRepository = consultantProfileRepository;
    }

    public async Task<Result> HandleAsync(
        LogoutCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.UserId == Guid.Empty)
            return Result.Failure("شناسه کاربر معتبر نیست");

        var profile = await consultantProfileRepository.GetAll()
            .FirstOrDefaultAsync(x => x.UserId == command.UserId && !x.IsDeleted, cancellationToken);
        if (profile?.IsOnline == true)
        {
            profile.IsOnline = false;
            profile.LastOfflineAt = DateTime.Now;
            consultantProfileRepository.Update(profile);
            await consultantProfileRepository.SaveChange();
        }

        await presenceService.LogAsync(
            command.UserId,
            UserPresenceEventType.Logout,
            cancellationToken: cancellationToken);

        return Result.Success("خروج با موفقیت ثبت شد");
    }
}
