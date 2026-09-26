using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.Domain.IRepositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using WebPush;

namespace DentalDashboard.ApplicationService.Services;

public class WebPushNotificationService : IPushNotificationService
{
    private readonly IPushSubscriptionRepository pushSubscriptionRepository;
    private readonly IConfiguration configuration;
    private readonly ILogger<WebPushNotificationService> logger;

    public WebPushNotificationService(
        IPushSubscriptionRepository pushSubscriptionRepository,
        IConfiguration configuration,
        ILogger<WebPushNotificationService> logger)
    {
        this.pushSubscriptionRepository = pushSubscriptionRepository;
        this.configuration = configuration;
        this.logger = logger;
    }

    public async Task<bool> SendAsync(
        Guid userId,
        string title,
        string body,
        IReadOnlyDictionary<string, string>? data = null,
        CancellationToken cancellationToken = default)
    {
        var subscriptions = await pushSubscriptionRepository.GetActiveByUserIdAsync(
            userId,
            cancellationToken);

        if (subscriptions.Count == 0)
        {
            logger.LogWarning(
                "Push notification skipped for user {UserId}: no active subscriptions",
                userId);
            return false;
        }

        var vapidDetails = TryGetVapidDetails();
        if (vapidDetails == null)
        {
            logger.LogWarning(
                "Push notification skipped for user {UserId}: WebPush VAPID keys are not configured",
                userId);
            return false;
        }

        var payload = JsonSerializer.Serialize(new
        {
            title,
            body,
            data = data ?? new Dictionary<string, string>(),
        });

        var client = new WebPushClient();
        var delivered = false;
        var invalidEndpoints = new List<string>();

        foreach (var item in subscriptions)
        {
            try
            {
                var subscription = new PushSubscription(
                    item.Endpoint,
                    item.P256dh,
                    item.Auth);

                await client.SendNotificationAsync(subscription, payload, vapidDetails);
                delivered = true;
            }
            catch (WebPushException ex) when (IsInvalidSubscription(ex))
            {
                logger.LogWarning(
                    ex,
                    "WebPush rejected subscription for user {UserId}: {StatusCode}",
                    userId,
                    ex.StatusCode);
                invalidEndpoints.Add(item.Endpoint);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Push notification failed for user {UserId}", userId);
            }
        }

        foreach (var endpoint in invalidEndpoints.Distinct(StringComparer.Ordinal))
        {
            var stale = subscriptions.FirstOrDefault(x => x.Endpoint == endpoint);
            if (stale == null)
                continue;

            stale.IsDeleted = true;
            stale.UpdatedAt = DateTime.UtcNow;
            pushSubscriptionRepository.Update(stale);
        }

        if (invalidEndpoints.Count > 0)
            await pushSubscriptionRepository.SaveChange();

        return delivered;
    }

    private VapidDetails? TryGetVapidDetails()
    {
        var publicKey = configuration["WebPush:VapidPublicKey"]
                        ?? Environment.GetEnvironmentVariable("WEBPUSH_VAPID_PUBLIC_KEY");
        var privateKey = configuration["WebPush:VapidPrivateKey"]
                         ?? Environment.GetEnvironmentVariable("WEBPUSH_VAPID_PRIVATE_KEY");
        var subject = configuration["WebPush:VapidSubject"]
                      ?? Environment.GetEnvironmentVariable("WEBPUSH_VAPID_SUBJECT")
                      ?? "mailto:support@drmoghadam.com";

        if (string.IsNullOrWhiteSpace(publicKey) || string.IsNullOrWhiteSpace(privateKey))
            return null;

        return new VapidDetails(subject.Trim(), publicKey.Trim(), privateKey.Trim());
    }

    private static bool IsInvalidSubscription(WebPushException exception)
    {
        return exception.StatusCode is HttpStatusCode.Gone or HttpStatusCode.NotFound
               or HttpStatusCode.BadRequest;
    }
}
