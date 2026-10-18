using DentalDashboard.LeadManagement.Application;
using DentalDashboard.LeadManagement.BackgroundServices;
using DentalDashboard.LeadManagement.Configuration;
using DentalDashboard.LeadManagement.Contract;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace DentalDashboard.LeadManagement.Infrastructure;
public static class LeadManagementServiceRegistration
{
    public static IServiceCollection AddLeadManagement(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<LeadManagementOptions>()
            .Configure(options =>
            {
                var section = configuration.GetSection("LeadManagement");

                if (int.TryParse(section["IntervalSeconds"], out var intervalSeconds))
                    options.IntervalSeconds = intervalSeconds;

                if (int.TryParse(section["AdminSheetBatchSize"], out var batchSize))
                    options.AdminSheetBatchSize = batchSize;
            });
        services.AddScoped<ILeadImportCoordinator, LeadImportCoordinator>();
        services.AddScoped<IAdminLeadSheetService, AdminLeadSheetService>();
        services.AddHostedService<LeadImportBackgroundService>();
        return services;
    }
}
