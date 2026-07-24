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
    private readonly IUserRepository userRepository;
    private readonly IAttendanceService attendanceService;
    private readonly IPushSubscriptionRepository pushSubscriptionRepository;

    public LogoutCommandHandler(
        IUserPresenceService presenceService,
        IConsultantProfileRepository consultantProfileRepository,
        IUserRepository userRepository,
        IAttendanceService attendanceService,
        IPushSubscriptionRepository pushSubscriptionRepository)
    {
        this.presenceService = presenceService;
        this.consultantProfileRepository = consultantProfileRepository;
        this.userRepository = userRepository;
        this.attendanceService = attendanceService;
        this.pushSubscriptionRepository = pushSubscriptionRepository;
    }

    public async Task<Result> HandleAsync(
        LogoutCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.UserId == Guid.Empty)
            return Result.Failure("شناسه کاربر معتبر نیست");

        var now = DateTime.Now;
        var utcNow = DateTime.UtcNow;

        await presenceService.LogAsync(
            command.UserId,
            UserPresenceEventType.Logout,
            cancellationToken: cancellationToken);

        var profile = await consultantProfileRepository.GetAll()
            .FirstOrDefaultAsync(
                x => x.UserId == command.UserId && !x.IsDeleted,
                cancellationToken);

        if (profile != null)
        {
            var wasOnline = profile.IsOnline;
            var wasAvailable = profile.IsAvailable;

            if (wasOnline)
            {
                profile.IsOnline = false;
                profile.LastOfflineAt = now;

                await presenceService.LogAsync(
                    command.UserId,
                    UserPresenceEventType.Offline,
                    profile.LastOfflineAt,
                    cancellationToken: cancellationToken);
            }

            if (wasAvailable)
            {
                profile.IsAvailable = false;
                profile.WorkEndTime = now.TimeOfDay;
                profile.LastOfflineAt = now;

                await attendanceService.RecordCheckOutAsync(
                    profile.Id,
                    now,
                    cancellationToken);
            }

            if (wasOnline || wasAvailable)
            {
                consultantProfileRepository.Update(profile);
                await consultantProfileRepository.SaveChange();
            }
        }

        var user = await userRepository.GetAll()
            .FirstOrDefaultAsync(x => x.Id == command.UserId && !x.IsDeleted, cancellationToken);

        if (user != null)
        {
            user.LastSeenAt = null;
            user.PushNotificationToken = null;
            user.UpdatedAt = utcNow;
            userRepository.Update(user);
            await userRepository.SaveChange();
        }

        await pushSubscriptionRepository.DeactivateAllByUserIdAsync(
            command.UserId,
            cancellationToken);

        return Result.Success("خروج با موفقیت ثبت شد");
    }
}
