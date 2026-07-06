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
                using var scope = scopeFactory.CreateScope();
                var leadAssignmentService =
                    scope.ServiceProvider.GetRequiredService<ILeadAssignmentService>();

                // Each step is isolated so a failure in lead import does not skip assignment.
                await RunStepAsync("AddLeads", () => leadAssignmentService.AddLeadsAsync(), stoppingToken);
                await RunStepAsync("AssignOfflineLeads", () => leadAssignmentService.AssignOfflineLeadsAsync(), stoppingToken);
                await RunStepAsync("AssignRealTimeLeads", () => leadAssignmentService.AssignRealTimeLeadsAsync(), stoppingToken);
                await RunStepAsync("ExpireOverdueRealTimeLeads", () => leadAssignmentService.ExpireOverdueRealTimeLeadsAsync(), stoppingToken);
                await RunStepAsync("EnforceNightShiftClosure", () => leadAssignmentService.EnforceNightShiftClosureAsync(), stoppingToken);

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        private async Task RunStepAsync(string stepName, Func<Task> step, CancellationToken stoppingToken)
        {
            try
            {
                logger.LogInformation("Lead assignment cycle step started: {StepName}", stepName);
                await step();
                logger.LogInformation("Lead assignment cycle step completed: {StepName}", stepName);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Lead assignment cycle step failed: {StepName}", stepName);
            }
        }
    }
}
