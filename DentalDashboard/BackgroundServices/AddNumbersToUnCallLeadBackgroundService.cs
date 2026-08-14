using DentalDashboard.ApplicationService.Contract.IServices;

namespace DentalDashboard.BackgroundServices
{
    public class AddNumbersToUnCallLeadBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory serviceScopeFactory;

        public AddNumbersToUnCallLeadBackgroundService(IServiceScopeFactory serviceScopeFactory)
        {
            this.serviceScopeFactory = serviceScopeFactory;
        }

        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var scope = serviceScopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<ILeadAssignmentService>();
            await service.AddLeadToUnCallLead();
            await Task.Delay(TimeSpan.FromDays(15));
        }
    }
}
