using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.Domain.IRepositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using WebPush;

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


    public async Task<bool> SendAsync(Guid userId,string title,string body,CancellationToken cancellationToken = default)
    {
        var subscriptions =await pushSubscriptionRepository.GetByUserIdAsync(userId,cancellationToken);


        if (!subscriptions.Any())
            return false;

        var vapid = new VapidDetails(
            configuration["WebPush:VapidSubject"],
            configuration["WebPush:VapidPublicKey"],
            configuration["WebPush:VapidPrivateKey"]);

        var client = new WebPushClient();

        var payload = JsonSerializer.Serialize(new
        {
            title,
            body
        });


        foreach (var item in subscriptions)
        {
            try
            {
                var subscription = new PushSubscription(
                    item.Endpoint,
                    item.P256dh,
                    item.Auth);


                await client.SendNotificationAsync(
                    subscription,
                    payload,
                    vapid);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Push notification failed for user {UserId}",
                    userId);
            }
        }


        return true;
    }
}