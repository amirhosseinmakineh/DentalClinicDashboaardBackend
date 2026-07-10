using DentalDashboard.ApplicationService.Contract.IServices;

namespace DentalDashboard.BackgroundServices;

public class AddLeadBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<AddLeadBackgroundService> logger;

    public AddLeadBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<AddLeadBackgroundService> logger)
    {
        this.scopeFactory = scopeFactory;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();

                var leadService =
                    scope.ServiceProvider
                        .GetRequiredService<ILeadAssignmentService>();

                logger.LogInformation("AddLeads cycle started");

                await leadService.AddLeadsAsync();

                logger.LogInformation("AddLeads cycle completed");
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "AddLeadBackgroundService failed");
            }

            await Task.Delay(
                TimeSpan.FromSeconds(10),
                stoppingToken);
        }
    }
}