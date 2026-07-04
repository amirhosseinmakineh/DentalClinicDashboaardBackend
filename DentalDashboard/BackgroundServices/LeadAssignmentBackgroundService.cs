using DentalDashboard.ApplicationService.Contract.IServices;

namespace DentalDashboard.BackgroundServices
{
    public class LeadAssignmentBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory scopeFactory;
        private readonly ILogger<LeadAssignmentBackgroundService> logger;

        public LeadAssignmentBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<LeadAssignmentBackgroundService> logger)
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

                    var leadAssignmentService =
                        scope.ServiceProvider.GetRequiredService<ILeadAssignmentService>();

                    await leadAssignmentService.AddLeadsAsync();
                    await leadAssignmentService.ExpireStaleBroadcastsAsync();
                    await leadAssignmentService.AssignPendingOfflineLeadsAsync();
                    await leadAssignmentService.BroadcastRealTimeLeadsAsync();
                    await leadAssignmentService.ExpireOverdueRealTimeLeadsAsync();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Lead assignment cycle failed");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}