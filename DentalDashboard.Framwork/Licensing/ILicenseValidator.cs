namespace DentalDashboard.Framwork.Licensing;

public interface ILicenseValidator
{
    ValueTask<LicenseValidationResult> ValidateAsync(
        SignedLicense license,
        LicenseInstallation installation,
        CancellationToken cancellationToken = default);
}
