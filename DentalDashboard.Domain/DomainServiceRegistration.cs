using DentalDashboard.Domain.DomainServices;
using DentalDashboard.Domain.IDomainService;
using Microsoft.Extensions.DependencyInjection;

namespace DentalDashboard.Domain;

public static class DomainServiceRegistration
{
    public static IServiceCollection AddDomainServices(this IServiceCollection services)
    {
        services.AddScoped<ILeadAssignmentStrategy, ScoreBasedRoundRobinLeadAssignmentStrategy>();
        services.AddScoped<IOfflineLeadAssignmentStrategy, OfflineLeadAssignmentStrategy>();
        services.AddScoped<ILeadDomainService, LeadDomainService>();
        services.AddScoped<ILeadReportDomainService, LeadReportDomainService>();

        return services;
    }
}
