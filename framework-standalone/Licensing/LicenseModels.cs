namespace DentalDashboard.Framwork.Licensing;

public sealed record LicenseInstallation(
    string InstallationId,
    string ProductId,
    string? Environment = null);

public sealed record SignedLicense(
    string LicenseId,
    string ProductId,
    DateTimeOffset ExpiresAt,
    string Payload,
    string Signature,
    string KeyId);

public sealed record LicenseValidationResult(
    bool IsValid,
    string? Reason = null,
    DateTimeOffset? ExpiresAt = null)
{
    public static LicenseValidationResult Valid(DateTimeOffset expiresAt) =>
        new(true, null, expiresAt);

    public static LicenseValidationResult Invalid(string reason) =>
        new(false, reason);
}
