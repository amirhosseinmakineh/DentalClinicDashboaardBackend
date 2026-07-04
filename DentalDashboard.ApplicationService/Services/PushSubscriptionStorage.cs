using System.Text.Json;

namespace DentalDashboard.ApplicationService.Services;

public static class PushSubscriptionStorage
{
    private const int MaxSubscriptions = 5;

    public static IReadOnlyList<string> ParseSubscriptions(string? stored)
    {
        if (string.IsNullOrWhiteSpace(stored))
            return Array.Empty<string>();

        var trimmed = stored.Trim();
        if (trimmed.StartsWith('['))
        {
            try
            {
                var items = JsonSerializer.Deserialize<List<string>>(trimmed) ?? new List<string>();
                return items
                    .Where(LooksLikePushSubscription)
                    .Distinct(StringComparer.Ordinal)
                    .ToList();
            }
            catch (JsonException)
            {
                return Array.Empty<string>();
            }
        }

        return LooksLikePushSubscription(trimmed)
            ? new[] { trimmed }
            : Array.Empty<string>();
    }

    public static string UpsertSubscription(string? stored, string subscriptionJson)
    {
        var normalized = subscriptionJson.Trim();
        if (!LooksLikePushSubscription(normalized))
            return stored?.Trim() ?? string.Empty;

        var endpoint = GetEndpoint(normalized);
        if (string.IsNullOrWhiteSpace(endpoint))
            return stored?.Trim() ?? string.Empty;

        var subscriptions = ParseSubscriptions(stored)
            .Where(item => !string.Equals(GetEndpoint(item), endpoint, StringComparison.Ordinal))
            .Prepend(normalized)
            .Take(MaxSubscriptions)
            .ToList();

        return subscriptions.Count == 1
            ? subscriptions[0]
            : JsonSerializer.Serialize(subscriptions);
    }

    public static string? RemoveSubscription(string? stored, string subscriptionJson)
    {
        var endpoint = GetEndpoint(subscriptionJson);
        if (string.IsNullOrWhiteSpace(endpoint))
            return stored;

        var subscriptions = ParseSubscriptions(stored)
            .Where(item => !string.Equals(GetEndpoint(item), endpoint, StringComparison.Ordinal))
            .ToList();

        return subscriptions.Count switch
        {
            0 => null,
            1 => subscriptions[0],
            _ => JsonSerializer.Serialize(subscriptions),
        };
    }

    public static bool LooksLikePushSubscription(string value)
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

    private static string? GetEndpoint(string subscriptionJson)
    {
        try
        {
            using var document = JsonDocument.Parse(subscriptionJson);
            if (!document.RootElement.TryGetProperty("endpoint", out var endpoint))
                return null;

            return endpoint.GetString();
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
