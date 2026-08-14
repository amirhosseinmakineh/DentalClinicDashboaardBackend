
using DentalDashboard.ApplicationService.Contract.IServices;
using Microsoft.Extensions.Logging;

namespace DentalDashboard.BackgroundServices
{
    public class TestConsultantLeadAssignmentBackgroundService : BackgroundService
    {

        private readonly IServiceScopeFactory scopeFactory;
        private readonly ILogger<TestConsultantLeadAssignmentBackgroundService> logger;

        public TestConsultantLeadAssignmentBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<TestConsultantLeadAssignmentBackgroundService> logger)
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
                await  leadAssignmentService.AssignLeadToTestConsultant();
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }
}
