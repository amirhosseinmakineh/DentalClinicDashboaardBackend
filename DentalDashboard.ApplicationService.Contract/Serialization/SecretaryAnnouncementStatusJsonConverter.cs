using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using DentalDashboard.Domain.Enums;

namespace DentalDashboard.ApplicationService.Contract.Serialization;

/// <summary>
/// Accepts the string values commonly produced by HTML selects while keeping the
/// public response value as the enum's canonical name.
/// </summary>
public sealed class SecretaryAnnouncementStatusJsonConverter
    : JsonConverter<SecretaryAnnouncementStatus>
{
    public override SecretaryAnnouncementStatus Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var numericStatus))
            return FromNumber(numericStatus);

        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("وضعیت اعلام منشی باید به صورت رشته یا عدد ارسال شود.");

        var value = reader.GetString()?.Trim();
        if (string.IsNullOrEmpty(value))
            throw new JsonException("وضعیت اعلام منشی الزامی است.");

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out numericStatus))
            return FromNumber(numericStatus);

        var normalizedValue = Normalize(value);
        foreach (var status in Enum.GetValues<SecretaryAnnouncementStatus>())
        {
            if (Normalize(status.ToString()) == normalizedValue)
                return status;
        }

        throw new JsonException("وضعیت اعلام منشی معتبر نیست.");
    }

    public override void Write(
        Utf8JsonWriter writer,
        SecretaryAnnouncementStatus value,
        JsonSerializerOptions options) => writer.WriteStringValue(value.ToString());

    private static SecretaryAnnouncementStatus FromNumber(int value)
    {
        if (Enum.IsDefined(typeof(SecretaryAnnouncementStatus), value))
            return (SecretaryAnnouncementStatus)value;

        throw new JsonException("وضعیت اعلام منشی معتبر نیست.");
    }

    private static string Normalize(string value) =>
        value.Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .ToUpperInvariant();
}
