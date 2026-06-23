using DentalDashboard.Security.Generator;
using Microsoft.Extensions.DependencyInjection;

namespace DentalDashboard.Security;

public static class SecurityServiceRegistration
{
    public static IServiceCollection AddSecurityServices(this IServiceCollection services)
    {
        services.AddTransient<ITokenGenerator, TokenGenerator>();

        return services;
    }
}
