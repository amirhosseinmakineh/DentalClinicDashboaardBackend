using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.ApplicationService.Contract.Requests.Consultant.Commands;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Handlers.CommandHandlers.Consultant;

public class SendTestPushNotificationCommandHandler : ICommandHandler<SendTestPushNotificationCommand>
{
    private readonly IConsultantProfileRepository consultantProfileRepository;
    private readonly IPushNotificationService pushNotificationService;

    public SendTestPushNotificationCommandHandler(
        IConsultantProfileRepository consultantProfileRepository,
        IPushNotificationService pushNotificationService)
    {
        this.consultantProfileRepository = consultantProfileRepository;
        this.pushNotificationService = pushNotificationService;
    }

    public async Task<Result> HandleAsync(
        SendTestPushNotificationCommand command,
        CancellationToken cancellationToken = default)
    {
        var profile = await consultantProfileRepository.GetAll()
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == command.ProfileId, cancellationToken);

        if (profile == null || profile.IsDeleted)
            return Result.Failure("مشاوری یافت نشد");

        var sent = await pushNotificationService.SendAsync(
            profile.UserId,
            "تست نوتیفیکیشن",
            "اگر این پیام را می‌بینید، Web Push روی PWA شما فعال است.",
            new Dictionary<string, string>
            {
                ["type"] = "test_push",
                ["profileId"] = profile.Id.ToString()
            },
            cancellationToken);

        return sent
            ? Result.Success("نوتیفیکیشن تست ارسال شد")
            : Result.Failure(
                "ارسال نوتیفیکیشن انجام نشد. ابتدا اجازه Notification را بدهید و PWA را یک‌بار باز کنید.");
    }
}
