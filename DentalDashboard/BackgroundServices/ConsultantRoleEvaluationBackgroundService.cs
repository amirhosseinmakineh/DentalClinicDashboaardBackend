using DentalDashboard.ApplicationService.Contract.IServices;

namespace DentalDashboard.BackgroundServices;

public sealed class ConsultantRoleEvaluationBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<ConsultantRoleEvaluationBackgroundService> logger;

    public ConsultantRoleEvaluationBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<ConsultantRoleEvaluationBackgroundService> logger)
    {
        this.scopeFactory = scopeFactory;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(15));
        do
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var service = scope.ServiceProvider.GetRequiredService<IConsultantRoleEvaluationService>();
                await service.EvaluateDueConsultantsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Consultant role evaluation cycle failed");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
