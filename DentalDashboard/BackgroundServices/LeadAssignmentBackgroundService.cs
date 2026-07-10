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
                await RunStepAsync("AssignRealTimeLeads", () => leadAssignmentService.AssignRealTimeLeadsAsync(), stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

                //await RunStepAsync(
                //    "ReconcileLeadStates",
                //    () => leadAssignmentService.ReconcileMisclassifiedLeadStatesAsync(),
                //    stoppingToken);
                //await RunStepAsync("ExpireOverdueRealTimeLeads", () => leadAssignmentService.ExpireOverdueRealTimeLeadsAsync(), stoppingToken);

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
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Lead assignment cycle step failed: {StepName}", stepName);
            }
        }
    }
}
