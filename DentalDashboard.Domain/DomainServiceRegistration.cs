using DentalDashboard.Domain.DomainServices;
using DentalDashboard.Domain.IDomainService;
using Microsoft.Extensions.DependencyInjection;
using DentalDashboard.Domain.RolePolicies;

namespace DentalDashboard.Domain;

public static class DomainServiceRegistration
{
    public static IServiceCollection AddDomainServices(this IServiceCollection services)
    {
        services.AddScoped<ILeadDomainService, LeadDomainService>();
        services.AddScoped<ILeadReportDomainService, LeadReportDomainService>();
        services.AddSingleton<IConsultantRolePolicyProvider, ConsultantRolePolicyProvider>();

        return services;
    }
}
