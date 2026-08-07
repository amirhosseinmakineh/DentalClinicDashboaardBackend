using DentalDashboard.ApplicationService.Contract.Requests.Consultant.Commands;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;

namespace DentalDashboard.BackgroundServices;

public sealed class RoleBasedLeadDistributionBackgroundService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(10);
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<RoleBasedLeadDistributionBackgroundService> logger;

    public RoleBasedLeadDistributionBackgroundService(IServiceScopeFactory scopeFactory,
        ILogger<RoleBasedLeadDistributionBackgroundService> logger)
    {
        this.scopeFactory = scopeFactory; this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
                await commands.DispatchAsync(new ProcessRoleBasedRealtimeLeadsCommand(), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "Role-based realtime distribution cycle failed"); }
            await Task.Delay(Interval, stoppingToken);
        }
    }
}
