using DentalDashboard.ApplicationService.Contract.IServices;

namespace DentalDashboard.BackgroundServices
{
    public class LeadAssignmentBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory scopeFactory;

        public LeadAssignmentBackgroundService(IServiceScopeFactory scopeFactory)
        {
            this.scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = scopeFactory.CreateScope();

                var leadAssignmentService =
                    scope.ServiceProvider.GetRequiredService<ILeadAssignmentService>();

                await leadAssignmentService.AddLeadsAsync();
                await leadAssignmentService.AssignPendingOfflineLeadsAsync();
                await leadAssignmentService.AssignRealTimeLeadsAsync();
                await leadAssignmentService.ExpireOverdueRealTimeLeadsAsync();

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}