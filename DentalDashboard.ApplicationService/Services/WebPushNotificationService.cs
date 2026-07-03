using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.Domain.IRepositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using WebPush;

namespace DentalDashboard.ApplicationService.Services;

public class WebPushNotificationService : IPushNotificationService
{
    private readonly IUserRepository userRepository;
    private readonly IConfiguration configuration;
    private readonly ILogger<WebPushNotificationService> logger;

    public WebPushNotificationService(
        IUserRepository userRepository,
        IConfiguration configuration,
        ILogger<WebPushNotificationService> logger)
    {
        this.userRepository = userRepository;
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
        var subscriptionJson = await GetSubscriptionJsonAsync(userId, cancellationToken);
        if (subscriptionJson == null)
            return false;

        var vapidDetails = TryGetVapidDetails();
        if (vapidDetails == null)
        {
            logger.LogWarning(
                "Push notification skipped for user {UserId}: WebPush VAPID keys are not configured",
                userId);
            return false;
        }

        if (!TryParseSubscription(subscriptionJson, out var pushSubscription))
        {
            logger.LogWarning(
                "Push notification skipped for user {UserId}: stored push subscription is invalid",
                userId);
            return false;
        }

        var payload = JsonSerializer.Serialize(new
        {
            title,
            body,
            data = data ?? new Dictionary<string, string>()
        });

        try
        {
            var client = new WebPushClient();
            await client.SendNotificationAsync(pushSubscription, payload, vapidDetails);
            return true;
        }
        catch (WebPushException ex) when (IsInvalidSubscription(ex))
        {
            logger.LogWarning(
                ex,
                "WebPush rejected subscription for user {UserId}: {StatusCode}",
                userId,
                ex.StatusCode);
            await ClearSubscriptionAsync(userId, cancellationToken);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "WebPush request failed for user {UserId}", userId);
            return false;
        }
    }

    private async Task<string?> GetSubscriptionJsonAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetAll()
            .Where(x => x.Id == userId && !x.IsDeleted)
            .Select(x => new { x.Id, x.PushNotificationToken })
            .FirstOrDefaultAsync(cancellationToken);

        if (user == null || string.IsNullOrWhiteSpace(user.PushNotificationToken))
        {
            logger.LogWarning(
                "Push notification skipped for user {UserId}: push subscription is missing",
                userId);
            return null;
        }

        return user.PushNotificationToken.Trim();
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

    private static bool TryParseSubscription(
        string subscriptionJson,
        out PushSubscription pushSubscription)
    {
        pushSubscription = null!;

        try
        {
            using var document = JsonDocument.Parse(subscriptionJson);
            var root = document.RootElement;

            if (!root.TryGetProperty("endpoint", out var endpointProperty))
                return false;

            var endpoint = endpointProperty.GetString();
            if (string.IsNullOrWhiteSpace(endpoint))
                return false;

            if (!root.TryGetProperty("keys", out var keysProperty))
                return false;

            if (!keysProperty.TryGetProperty("p256dh", out var p256dhProperty) ||
                !keysProperty.TryGetProperty("auth", out var authProperty))
            {
                return false;
            }

            var p256dh = p256dhProperty.GetString();
            var auth = authProperty.GetString();
            if (string.IsNullOrWhiteSpace(p256dh) || string.IsNullOrWhiteSpace(auth))
                return false;

            pushSubscription = new PushSubscription(endpoint, p256dh, auth);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool IsInvalidSubscription(WebPushException exception)
    {
        return exception.StatusCode is HttpStatusCode.Gone or HttpStatusCode.NotFound
               or HttpStatusCode.BadRequest;
    }

    private async Task ClearSubscriptionAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetAll()
            .FirstOrDefaultAsync(x => x.Id == userId && !x.IsDeleted, cancellationToken);

        if (user == null || string.IsNullOrWhiteSpace(user.PushNotificationToken))
            return;

        user.PushNotificationToken = null;
        user.UpdatedAt = DateTime.UtcNow;
        userRepository.Update(user);
        await userRepository.SaveChange();
        logger.LogInformation("Cleared invalid push subscription for user {UserId}", userId);
    }
}
