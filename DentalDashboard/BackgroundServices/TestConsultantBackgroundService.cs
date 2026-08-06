using DentalDashboard.ApplicationService.Contract.Requests.Consultant.Commands;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;

namespace DentalDashboard.BackgroundServices;

public sealed class TestConsultantBackgroundService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(10);
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<TestConsultantBackgroundService> logger;

    public TestConsultantBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<TestConsultantBackgroundService> logger)
    {
        this.scopeFactory = scopeFactory;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var dispatcher = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
                await dispatcher.DispatchAsync(new ProcessTestConsultantsCommand(), stoppingToken);
                await dispatcher.DispatchAsync(new EvaluateTestConsultantsCommand(), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "TEST consultant processing cycle failed");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }
}
