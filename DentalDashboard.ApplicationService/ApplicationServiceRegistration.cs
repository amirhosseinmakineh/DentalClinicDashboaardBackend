using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.ApplicationService.Services;
using DentalDashboard.Framwork.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace DentalDashboard.ApplicationService;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IRoleService, RoleService>();
        services.AddHttpClient<ILeadAssignmentService, LeadAssignmentService>()
            .ConfigureHttpClient(client => client.Timeout = TimeSpan.FromMinutes(5))
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip |
                                         DecompressionMethods.Deflate |
                                         DecompressionMethods.Brotli,
                ConnectTimeout = TimeSpan.FromSeconds(30),
                PooledConnectionLifetime = TimeSpan.FromMinutes(10)
            });
        services.AddScoped<IPushNotificationService, WebPushNotificationService>();
        services.AddScoped<IConsultantProfileService, ConsultantProfileService>();
        services.AddScoped<IUserPresenceService, UserPresenceService>();
        services.AddScoped<IAttendanceService, AttendanceService>();
        services.AddScoped<IPickupService, PickUpService>();
        services.AddScoped<ILeadAssignmentCandidateProvider, LeadAssignmentCandidateProvider>();
        services.AddScoped<IQueryDispatcher, QueryDispatcher>();
        services.AddTransient<ICommandDispatcher, CommandDispatcher>();

        services.AddCqrsTypesFromAssembly(typeof(ApplicationServiceRegistration).Assembly);

        return services;
    }
}
