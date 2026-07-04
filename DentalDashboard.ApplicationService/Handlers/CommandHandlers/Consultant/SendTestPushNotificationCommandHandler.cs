using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.ApplicationService.Contract.Requests.Consultant.Commands;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

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
            profile.User.PushNotificationToken = command.DeviceToken.Trim();
            profile.User.UpdatedAt = DateTime.UtcNow;
            await consultantProfileRepository.SaveChange();
        }

        if (string.IsNullOrWhiteSpace(profile.User.PushNotificationToken))
        {
            return Result.Failure(
                "subscription روی سرور ثبت نشده است. ابتدا «فعال‌سازی نوتیفیکیشن» را بزنید.");
        }

        if (!LooksLikePushSubscription(profile.User.PushNotificationToken))
        {
            return Result.Failure(
                "subscription ذخیره‌شده روی سرور معتبر نیست. دوباره «فعال‌سازی نوتیفیکیشن» را بزنید.");
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
            ? Result.Success("نوتیفیکیشن تست ارسال شد")
            : Result.Failure(
                "ارسال push انجام نشد. WEBPUSH_VAPID_PRIVATE_KEY و WEBPUSH_VAPID_PUBLIC_KEY را روی سرور و Netlify بررسی کنید.");
    }

    private static bool LooksLikePushSubscription(string value)
    {
        try
        {
            using var document = JsonDocument.Parse(value);
            return document.RootElement.TryGetProperty("endpoint", out var endpoint) &&
                   !string.IsNullOrWhiteSpace(endpoint.GetString());
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
