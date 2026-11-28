using DentalDashboard.ApplicationService.Accounting.Reminders;

namespace DentalDashboard.BackgroundServices.Accounting;

public sealed class AccountingReminderBackgroundService(IServiceScopeFactory scopeFactory,
    ILogger<AccountingReminderBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        do
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                await scope.ServiceProvider.GetRequiredService<AccountingReminderDispatcher>().DispatchAsync(DateTime.UtcNow, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "Accounting reminder scheduled check failed"); }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
