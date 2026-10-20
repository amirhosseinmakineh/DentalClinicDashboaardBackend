using DentalDashboard.Framwork.Licensing;

namespace DentalDashboard.Licensing;

public sealed class LicenseRefreshBackgroundService(
    LicenseClient client,
    ILogger<LicenseRefreshBackgroundService> logger) : BackgroundService
{
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await RefreshAsync(cancellationToken);
        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RefreshAsync(stoppingToken);
        }
    }

    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        try
        {
            await client.RefreshAsync(cancellationToken);
            logger.LogInformation("License check completed. Valid: {Valid}; reason: {Reason}",
                client.Status.IsValid, client.Status.Reason);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception,
                "License refresh failed. Only an unexpired, signed cached lease permits operation.");
        }
    }
}
