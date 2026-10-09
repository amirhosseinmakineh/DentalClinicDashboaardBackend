using DentalDashboard.LeadManagement.Application;
using DentalDashboard.LeadManagement.BackgroundServices;
using DentalDashboard.LeadManagement.Configuration;
using DentalDashboard.LeadManagement.Contract;
using DentalDashboard.LeadManagement.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace DentalDashboard.LeadManagement.Infrastructure;
public static class LeadManagementServiceRegistration
{
    public static IServiceCollection AddLeadManagement(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<LeadManagementOptions>(configuration.GetSection("LeadManagement"));
        services.AddScoped<ILeadImportCoordinator, LeadImportCoordinator>();
        services.AddScoped<IAdminLeadSheetService, AdminLeadSheetService>();
        services.AddHostedService<LeadImportBackgroundService>();
        return services;
    }
}
