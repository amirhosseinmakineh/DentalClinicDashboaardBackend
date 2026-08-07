using DentalDashboard.ApplicationService.Contract.Requests.Consultant.Commands;
using DentalDashboard.ApplicationService.Contract.Requests.Consultant.Queries;
using DentalDashboard.ApplicationService.Contract.Responses.ConsultantResponse;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;

namespace DentalDashboard.BackgroundServices;

public sealed class SellerLeadDistributionBackgroundService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(10);
    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<SellerLeadDistributionBackgroundService> logger;

    public SellerLeadDistributionBackgroundService(IServiceScopeFactory scopeFactory,
        ILogger<SellerLeadDistributionBackgroundService> logger)
    {
        this.scopeFactory = scopeFactory; this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();
                var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
                var sellers = await queries.DispatchAsync<IReadOnlyList<ActiveSellerConsultantResponse>>(
                    new GetActiveSellerConsultantsQuery(), stoppingToken);
                await commands.DispatchAsync(
                    new DistributeSellerLeadsCommand(sellers.Select(x => x.ConsultantId).ToArray()),
                    stoppingToken);
                await commands.DispatchAsync(new EvaluateSellerConsultantsCommand(), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "Seller consultant processing cycle failed"); }
            await Task.Delay(Interval, stoppingToken);
        }
    }
}
