namespace DentalDashboard.Framwork.Licensing;

public interface ILicenseStateStore
{
    ValueTask<SignedLicense?> GetCachedLicenseAsync(
        LicenseInstallation installation,
        CancellationToken cancellationToken = default);

    ValueTask SaveCachedLicenseAsync(
        LicenseInstallation installation,
        SignedLicense license,
        CancellationToken cancellationToken = default);
}
