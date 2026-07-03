using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.Domain.IRepositories;
using Google.Apis.Auth.OAuth2;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace DentalDashboard.ApplicationService.Services
{
    public class FirebasePushNotificationService : IPushNotificationService
    {
        private const string FirebaseMessagingScope = "https://www.googleapis.com/auth/firebase.messaging";

        private readonly HttpClient httpClient;
        private readonly IUserRepository userRepository;
        private readonly IConfiguration configuration;
        private readonly ILogger<FirebasePushNotificationService> logger;
        private GoogleCredential? messagingCredential;

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

        public async Task<bool> SendAsync(
            Guid userId,
            string title,
            string body,
            IReadOnlyDictionary<string, string>? data = null,
            CancellationToken cancellationToken = default)
        {
            var deviceToken = await GetDeviceTokenAsync(userId, cancellationToken);
            if (deviceToken == null)
                return false;

            if (TryGetV1Settings(out var resolvedProjectId, out var serviceAccountJson))
            {
                return await SendViaV1Async(
                    resolvedProjectId,
                    serviceAccountJson,
                    deviceToken,
                    title,
                    body,
                    data,
                    userId,
                    cancellationToken);
            }

            var serverKey = configuration["Firebase:ServerKey"]
                            ?? Environment.GetEnvironmentVariable("FIREBASE_SERVER_KEY");

            if (!string.IsNullOrWhiteSpace(serverKey))
            {
                return await SendViaLegacyAsync(
                    serverKey,
                    deviceToken,
                    title,
                    body,
                    data,
                    userId,
                    cancellationToken);
            }

            logger.LogWarning(
                "Push notification skipped for user {UserId}: Firebase is not configured (set FIREBASE_SERVICE_ACCOUNT_JSON + FIREBASE_PROJECT_ID or FIREBASE_SERVER_KEY)",
                userId);
            return false;
        }

        private async Task<string?> GetDeviceTokenAsync(Guid userId, CancellationToken cancellationToken)
        {
            var user = await userRepository.GetAll()
                .Where(x => x.Id == userId && !x.IsDeleted)
                .Select(x => new { x.Id, x.PushNotificationToken })
                .FirstOrDefaultAsync(cancellationToken);

            if (user == null || string.IsNullOrWhiteSpace(user.PushNotificationToken))
            {
                logger.LogWarning("Push notification skipped for user {UserId}: device token is missing", userId);
                return null;
            }

            return user.PushNotificationToken;
        }

        private bool TryGetV1Settings(out string resolvedProjectId, out string serviceAccountJson)
        {
            serviceAccountJson = configuration["Firebase:ServiceAccountJson"]
                                 ?? Environment.GetEnvironmentVariable("FIREBASE_SERVICE_ACCOUNT_JSON")
                                 ?? string.Empty;

            resolvedProjectId = configuration["Firebase:ProjectId"]
                                ?? Environment.GetEnvironmentVariable("FIREBASE_PROJECT_ID")
                                ?? string.Empty;

            if (string.IsNullOrWhiteSpace(serviceAccountJson))
                return false;

            if (string.IsNullOrWhiteSpace(resolvedProjectId))
            {
                resolvedProjectId = TryReadProjectIdFromServiceAccount(serviceAccountJson) ?? string.Empty;
            }

            return !string.IsNullOrWhiteSpace(resolvedProjectId);
        }

        private static string? TryReadProjectIdFromServiceAccount(string serviceAccountJson)
        {
            try
            {
                using var document = JsonDocument.Parse(serviceAccountJson);
                if (document.RootElement.TryGetProperty("project_id", out var projectIdProperty))
                    return projectIdProperty.GetString();
            }
            catch (JsonException)
            {
            }

            return null;
        }

        private GoogleCredential GetMessagingCredential(string serviceAccountJson)
        {
            if (messagingCredential != null)
                return messagingCredential;

            messagingCredential = CredentialFactory
                .FromJson<ServiceAccountCredential>(serviceAccountJson)
                .ToGoogleCredential()
                .CreateScoped(FirebaseMessagingScope);

            return messagingCredential;
        }

        private async Task<bool> SendViaV1Async(
            string resolvedProjectId,
            string serviceAccountJson,
            string deviceToken,
            string title,
            string body,
            IReadOnlyDictionary<string, string>? data,
            Guid userId,
            CancellationToken cancellationToken)
        {
            string accessToken;
            try
            {
                var credential = GetMessagingCredential(serviceAccountJson);
                accessToken = await credential.UnderlyingCredential.GetAccessTokenForRequestAsync(cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to obtain FCM v1 access token for user {UserId}", userId);
                return false;
            }

            if (string.IsNullOrWhiteSpace(accessToken))
            {
                logger.LogWarning("FCM v1 access token is empty for user {UserId}", userId);
                return false;
            }

            var payload = BuildV1Payload(deviceToken, title, body, data);
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"https://fcm.googleapis.com/v1/projects/{resolvedProjectId}/messages:send");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Content = JsonContent.Create(payload);

            HttpResponseMessage response;
            try
            {
                response = await httpClient.SendAsync(request, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "FCM v1 request failed for user {UserId}", userId);
                return false;
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "FCM v1 HTTP error for user {UserId}: {StatusCode} {ResponseBody}",
                    userId,
                    response.StatusCode,
                    responseBody);

                if (IsInvalidTokenV1Response(responseBody))
                    await ClearPushTokenAsync(userId, cancellationToken);

                return false;
            }

            if (IsV1SuccessResponse(responseBody))
                return true;

            logger.LogWarning("FCM v1 rejected notification for user {UserId}: {ResponseBody}", userId, responseBody);
            return false;
        }

        private static object BuildV1Payload(
            string deviceToken,
            string title,
            string body,
            IReadOnlyDictionary<string, string>? data)
        {
            var dataPayload = data ?? new Dictionary<string, string>();

            return new
            {
                message = new
                {
                    token = deviceToken,
                    notification = new { title, body },
                    data = dataPayload,
                    android = new { priority = "HIGH" },
                    webpush = new
                    {
                        headers = new Dictionary<string, string>
                        {
                            ["Urgency"] = "high",
                            ["TTL"] = "86400"
                        },
                        notification = new
                        {
                            title,
                            body,
                            icon = "/icons/icon-192x192.png",
                            badge = "/icons/icon-96x96.png"
                        },
                        fcmOptions = new
                        {
                            link = ResolveWebPushLink(dataPayload)
                        }
                    }
                }
            };
        }

        private static string ResolveWebPushLink(IReadOnlyDictionary<string, string> dataPayload)
        {
            if (dataPayload.TryGetValue("type", out var type))
            {
                return type switch
                {
                    "password_changed" => "/",
                    "offline_leads" => "/dashboard/consultant?section=leads&type=offline",
                    "realtime_lead" => BuildRealtimeLeadLink(dataPayload),
                    _ => "/dashboard/consultant"
                };
            }

            return "/dashboard/consultant";
        }

        private static string BuildRealtimeLeadLink(IReadOnlyDictionary<string, string> dataPayload)
        {
            var baseUrl = "/dashboard/consultant?section=leads&type=realtime";
            if (dataPayload.TryGetValue("leadAssignmentId", out var leadAssignmentId) &&
                !string.IsNullOrWhiteSpace(leadAssignmentId))
            {
                return $"{baseUrl}&leadAssignmentId={Uri.EscapeDataString(leadAssignmentId)}";
            }

            return baseUrl;
        }

        private async Task<bool> SendViaLegacyAsync(
            string serverKey,
            string deviceToken,
            string title,
            string body,
            IReadOnlyDictionary<string, string>? data,
            Guid userId,
            CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://fcm.googleapis.com/fcm/send");
            request.Headers.TryAddWithoutValidation("Authorization", $"key={serverKey}");
            request.Content = JsonContent.Create(new
            {
                to = deviceToken,
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
                logger.LogError(ex, "FCM legacy request failed for user {UserId}", userId);
                return false;
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "FCM legacy HTTP error for user {UserId}: {StatusCode} {ResponseBody}",
                    userId,
                    response.StatusCode,
                    responseBody);
                return false;
            }

            if (TryGetLegacySuccessCount(responseBody, out var successCount) && successCount > 0)
                return true;

            logger.LogWarning("FCM legacy rejected notification for user {UserId}: {ResponseBody}", userId, responseBody);

            if (IsInvalidTokenLegacyResponse(responseBody))
                await ClearPushTokenAsync(userId, cancellationToken);

            return false;
        }

        private static bool IsV1SuccessResponse(string responseBody)
        {
            try
            {
                using var document = JsonDocument.Parse(responseBody);
                return document.RootElement.TryGetProperty("name", out var nameProperty) &&
                       !string.IsNullOrWhiteSpace(nameProperty.GetString());
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private static bool IsInvalidTokenV1Response(string responseBody)
        {
            if (string.IsNullOrWhiteSpace(responseBody))
                return false;

            if (responseBody.Contains("UNREGISTERED", StringComparison.OrdinalIgnoreCase) ||
                (responseBody.Contains("INVALID_ARGUMENT", StringComparison.OrdinalIgnoreCase) &&
                 responseBody.Contains("registration token", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            try
            {
                using var document = JsonDocument.Parse(responseBody);
                if (!document.RootElement.TryGetProperty("error", out var error))
                    return false;

                if (error.TryGetProperty("details", out var details) && details.ValueKind == JsonValueKind.Array)
                {
                    foreach (var detail in details.EnumerateArray())
                    {
                        if (detail.TryGetProperty("errorCode", out var errorCode) &&
                            string.Equals(errorCode.GetString(), "UNREGISTERED", StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }

                if (error.TryGetProperty("status", out var status) &&
                    string.Equals(status.GetString(), "NOT_FOUND", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            catch (JsonException)
            {
            }

            return false;
        }

        private static bool TryGetLegacySuccessCount(string responseBody, out int successCount)
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

        private static bool IsInvalidTokenLegacyResponse(string responseBody)
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
