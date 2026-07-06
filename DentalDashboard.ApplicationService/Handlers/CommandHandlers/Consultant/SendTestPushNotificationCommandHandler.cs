using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.ApplicationService.Contract.Requests.Consultant.Commands;
using DentalDashboard.ApplicationService.Services;
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

        if (!string.IsNullOrWhiteSpace(command.DeviceToken))
        {
            profile.User.PushNotificationToken = PushSubscriptionStorage.UpsertSubscription(
                profile.User.PushNotificationToken,
                command.DeviceToken.Trim());
            profile.User.UpdatedAt = DateTime.UtcNow;
            await consultantProfileRepository.SaveChange();
        }

        var subscriptions = PushSubscriptionStorage.ParseSubscriptions(
            profile.User.PushNotificationToken);
        if (subscriptions.Count == 0)
        {
            return Result.Failure(
                "subscription روی سرور ثبت نشده است. ابتدا «فعال‌سازی نوتیفیکیشن» را بزنید.");
        }

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
            ? Result.Success(
                "نوتیفیکیشن تست ارسال شد. اگر پیام سیستمی ندیدید، اجازه Notification را در مرورگر بررسی کنید.")
            : Result.Failure(
                "ارسال push انجام نشد. WEBPUSH_VAPID_PRIVATE_KEY و WEBPUSH_VAPID_PUBLIC_KEY را روی سرور و Netlify بررسی کنید.");
    }
}
