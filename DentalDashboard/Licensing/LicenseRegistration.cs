using DentalDashboard.Framwork.Licensing;
using Microsoft.AspNetCore.SignalR;

namespace DentalDashboard.Licensing;

public static class LicenseRegistration
{
    public static IServiceCollection AddClinicLicensing(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var options = configuration.GetSection("License").Get<LicenseClientOptions>()
            ?? new LicenseClientOptions();
        options.Environment ??= environment.EnvironmentName;
        options.Validate(environment.IsDevelopment());

        services.AddSingleton(options);
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ILicenseStateStore>(new FileLicenseStateStore(options.CachePath));
        services.AddSingleton<ILicenseValidator>(provider => new RsaLicenseValidator(
            options.PublicKeys, provider.GetRequiredService<TimeProvider>()));
        services.AddHttpClient("ClinicLicense", http => http.Timeout = TimeSpan.FromSeconds(10))
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                AllowAutoRedirect = false,
                PooledConnectionLifetime = TimeSpan.FromMinutes(5)
            });
        services.AddSingleton(provider => new LicenseClient(
            provider.GetRequiredService<IHttpClientFactory>().CreateClient("ClinicLicense"),
            options,
            provider.GetRequiredService<ILicenseStateStore>(),
            provider.GetRequiredService<ILicenseValidator>(),
            provider.GetRequiredService<TimeProvider>()));
        services.AddSingleton<ILicenseAccess>(provider => provider.GetRequiredService<LicenseClient>());
        services.AddHostedService<LicenseRefreshBackgroundService>();
        services.AddSignalR(options => options.AddFilter<LicenseHubFilter>());
        return services;
    }
}
