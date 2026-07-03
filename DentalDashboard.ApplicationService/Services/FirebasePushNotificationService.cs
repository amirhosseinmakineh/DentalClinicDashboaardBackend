using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.Domain.IRepositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;

namespace DentalDashboard.ApplicationService.Services
{
    public class FirebasePushNotificationService : IPushNotificationService
    {
        private readonly HttpClient httpClient;
        private readonly IUserRepository userRepository;
        private readonly IConfiguration configuration;
        private readonly ILogger<FirebasePushNotificationService> logger;

        public FirebasePushNotificationService(
            HttpClient httpClient,
            IUserRepository userRepository,
            IConfiguration configuration,
            ILogger<FirebasePushNotificationService> logger)
        {
            this.httpClient = httpClient;
            this.userRepository = userRepository;
            this.configuration = configuration;
            this.logger = logger;
        }

        public async Task<bool> SendAsync(Guid userId, string title, string body, IReadOnlyDictionary<string, string>? data = null, CancellationToken cancellationToken = default)
        {
            var serverKey = configuration["Firebase:ServerKey"]
                            ?? Environment.GetEnvironmentVariable("FIREBASE_SERVER_KEY");

            if (string.IsNullOrWhiteSpace(serverKey))
            {
                logger.LogWarning("Push notification skipped for user {UserId}: Firebase ServerKey is not configured", userId);
                return false;
            }

            var user = await userRepository.GetAll()
                .Where(x => x.Id == userId && !x.IsDeleted)
                .Select(x => new { x.Id, x.PushNotificationToken })
                .FirstOrDefaultAsync(cancellationToken);

            if (user == null || string.IsNullOrWhiteSpace(user.PushNotificationToken))
            {
                logger.LogWarning("Push notification skipped for user {UserId}: device token is missing", userId);
                return false;
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://fcm.googleapis.com/fcm/send");
            request.Headers.TryAddWithoutValidation("Authorization", $"key={serverKey}");
            request.Content = JsonContent.Create(new
            {
                to = user.PushNotificationToken,
                notification = new { title, body },
                data = data ?? new Dictionary<string, string>(),
                priority = "high"
            });

            HttpResponseMessage response;
            try
            {
                response = await httpClient.SendAsync(request, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "FCM request failed for user {UserId}", userId);
                return false;
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "FCM HTTP error for user {UserId}: {StatusCode} {ResponseBody}",
                    userId,
                    response.StatusCode,
                    responseBody);
                return false;
            }

            if (TryGetSuccessCount(responseBody, out var successCount) && successCount > 0)
                return true;

            logger.LogWarning("FCM rejected notification for user {UserId}: {ResponseBody}", userId, responseBody);

            if (IsInvalidTokenResponse(responseBody))
                await ClearPushTokenAsync(userId, cancellationToken);

            return false;
        }

        private static bool TryGetSuccessCount(string responseBody, out int successCount)
        {
            successCount = 0;
            try
            {
                using var document = JsonDocument.Parse(responseBody);
                if (document.RootElement.TryGetProperty("success", out var successProperty) &&
                    successProperty.TryGetInt32(out successCount))
                {
                    return true;
                }
            }
            catch (JsonException)
            {
            }

            return false;
        }

        private static bool IsInvalidTokenResponse(string responseBody)
        {
            return responseBody.Contains("NotRegistered", StringComparison.OrdinalIgnoreCase) ||
                   responseBody.Contains("InvalidRegistration", StringComparison.OrdinalIgnoreCase);
        }

        private async Task ClearPushTokenAsync(Guid userId, CancellationToken cancellationToken)
        {
            var user = await userRepository.GetAll()
                .FirstOrDefaultAsync(x => x.Id == userId && !x.IsDeleted, cancellationToken);

            if (user == null || string.IsNullOrWhiteSpace(user.PushNotificationToken))
                return;

            user.PushNotificationToken = null;
            userRepository.Update(user);
            await userRepository.SaveChange();
            logger.LogInformation("Cleared invalid push token for user {UserId}", userId);
        }
    }
}
