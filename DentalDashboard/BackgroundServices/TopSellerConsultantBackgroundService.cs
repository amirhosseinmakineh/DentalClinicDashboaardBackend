using DentalDashboard.ApplicationService.Contract.Requests.Consultant.Commands;
using DentalDashboard.ApplicationService.Contract.Requests.Consultant.Queries;
using DentalDashboard.ApplicationService.Contract.Responses.ConsultantResponse;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;

namespace DentalDashboard.BackgroundServices;

public sealed class TopSellerConsultantBackgroundService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(10);
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<TopSellerConsultantBackgroundService> logger;

    public TopSellerConsultantBackgroundService(IServiceScopeFactory scopeFactory,
        ILogger<TopSellerConsultantBackgroundService> logger)
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
                var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();
                var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
                var active = await queries.DispatchAsync<IReadOnlyList<ActiveTopSellerConsultantResponse>>(
                    new GetActiveTopSellerConsultantsQuery(), stoppingToken);
                logger.LogInformation("TopSeller processing started for {ConsultantCount} active consultants",
                    active.Count);
                await commands.DispatchAsync(new EvaluateTopSellerConsultantsCommand(), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "TopSeller processing cycle failed"); }
            await Task.Delay(Interval, stoppingToken);
        }
    }
}
