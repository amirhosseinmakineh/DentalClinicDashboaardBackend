using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.ApplicationService.Contract.Secretary.Accountant.Services;
using DentalDashboard.ApplicationService.Secretary.Accountant.Services;
using DentalDashboard.ApplicationService.Services;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using FluentValidation;
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
        services.AddScoped<IFinancialTransactionReceiptService, FinancialTransactionReceiptService>();

        services.AddScoped<IQueryDispatcher, QueryDispatcher>();
        services.AddTransient<ICommandDispatcher, CommandDispatcher>();

        services.RegisterHandlers(typeof(ApplicationServiceRegistration).Assembly, typeof(ICommandHandler<>));
        services.RegisterHandlers(typeof(ApplicationServiceRegistration).Assembly, typeof(ICommandHandler<,>));
        services.RegisterHandlers(typeof(ApplicationServiceRegistration).Assembly, typeof(IQueryHandler<,>));
        services.RegisterHandlers(typeof(ApplicationServiceRegistration).Assembly, typeof(IValidator<>));

        return services;
    }

    private static void RegisterHandlers(
        this IServiceCollection services,
        System.Reflection.Assembly assembly,
        Type openGenericHandlerType)
    {
        var handlerTypes = assembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .SelectMany(
                implementationType => implementationType
                    .GetInterfaces()
                    .Where(serviceType => serviceType.IsGenericType && serviceType.GetGenericTypeDefinition() == openGenericHandlerType)
                    .Select(serviceType => new { serviceType, implementationType }));

        foreach (var handlerType in handlerTypes)
        {
            services.AddTransient(handlerType.serviceType, handlerType.implementationType);
        }
    }
}
