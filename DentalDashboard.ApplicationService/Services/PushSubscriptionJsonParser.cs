using System.Text.Json;

namespace DentalDashboard.ApplicationService.Services;

public static class PushSubscriptionJsonParser
{
    public static bool TryParse(
        string subscriptionJson,
        out string endpoint,
        out string p256dh,
        out string auth)
    {
        endpoint = string.Empty;
        p256dh = string.Empty;
        auth = string.Empty;

        if (!PushSubscriptionStorage.LooksLikePushSubscription(subscriptionJson))
            return false;

        try
        {
            using var document = JsonDocument.Parse(subscriptionJson);
            var root = document.RootElement;

            if (!root.TryGetProperty("endpoint", out var endpointProperty))
                return false;

            endpoint = endpointProperty.GetString()?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(endpoint))
                return false;

            if (!root.TryGetProperty("keys", out var keysProperty))
                return false;

            if (!keysProperty.TryGetProperty("p256dh", out var p256dhProperty) ||
                !keysProperty.TryGetProperty("auth", out var authProperty))
            {
                return false;
            }

            p256dh = p256dhProperty.GetString()?.Trim() ?? string.Empty;
            auth = authProperty.GetString()?.Trim() ?? string.Empty;

            return !string.IsNullOrWhiteSpace(p256dh) && !string.IsNullOrWhiteSpace(auth);
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
