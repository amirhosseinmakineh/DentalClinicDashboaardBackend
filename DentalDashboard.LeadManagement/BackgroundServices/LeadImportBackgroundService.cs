using DentalDashboard.LeadManagement.Configuration;
using DentalDashboard.LeadManagement.Contract;
using Microsoft.Extensions.Options;
namespace DentalDashboard.LeadManagement.BackgroundServices;
public sealed class LeadImportBackgroundService(IServiceScopeFactory scopes, IOptions<LeadManagementOptions> options, ILogger<LeadImportBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(1, options.Value.IntervalSeconds));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                var coordinator = scope.ServiceProvider.GetRequiredService<ILeadImportCoordinator>();
                logger.LogInformation("Lead import cycle started. Order: Yektanet, AdminSheet");
                await coordinator.ProcessCycleAsync(stoppingToken);
                logger.LogInformation("Lead import cycle completed");
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "Lead import cycle failed; next cycle will continue"); }
            await Task.Delay(interval, stoppingToken);
        }
    }
}
