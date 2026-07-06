using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.ApplicationService.Services;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace DentalDashboard.ApplicationService;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IRoleService, RoleService>();
        services.AddHttpClient<ILeadAssignmentService, LeadAssignmentService>()
            .ConfigureHttpClient(client => client.Timeout = TimeSpan.FromSeconds(30));
        services.AddScoped<IPushNotificationService, WebPushNotificationService>();
        services.AddScoped<IConsultantProfileService, ConsultantProfileService>();

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
