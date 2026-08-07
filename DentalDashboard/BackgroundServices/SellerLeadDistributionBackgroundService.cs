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
                using var scope = scopeFactory.CreateScope();
                var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();
                var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
                var sellers = await queries.DispatchAsync<IReadOnlyList<ActiveSellerConsultantResponse>>(
                    new GetActiveSellerConsultantsQuery(), stoppingToken);
                foreach (var seller in sellers)
                {
                    try { await commands.DispatchAsync(new DistributeSellerLeadsCommand(seller.ConsultantId), stoppingToken); }
                    catch (Exception ex) { logger.LogError(ex, "Seller distribution failed for {ConsultantId}", seller.ConsultantId); }
                }
                await commands.DispatchAsync(new EvaluateSellerConsultantsCommand(), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "Seller consultant processing cycle failed"); }
            await Task.Delay(Interval, stoppingToken);
        }
    }
}
