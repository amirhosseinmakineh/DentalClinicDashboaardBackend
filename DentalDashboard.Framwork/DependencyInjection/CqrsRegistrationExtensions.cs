using System.Reflection;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace DentalDashboard.Framwork.DependencyInjection;

public static class CqrsRegistrationExtensions
{
    private static readonly Type[] CqrsServiceTypes =
    [
        typeof(ICommandHandler<>),
        typeof(ICommandHandler<,>),
        typeof(IQueryHandler<,>),
        typeof(IValidator<>)
    ];

    public static IServiceCollection AddCqrsTypesFromAssembly(
        this IServiceCollection services,
        Assembly assembly)
    {
        var registrations = assembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .SelectMany(implementationType => implementationType
                .GetInterfaces()
                .Where(IsCqrsService)
                .Select(serviceType => new
                {
                    ServiceType = serviceType,
                    ImplementationType = implementationType
                }));

        foreach (var registration in registrations)
        {
            services.AddTransient(
                registration.ServiceType,
                registration.ImplementationType);
        }

        return services;
    }

    private static bool IsCqrsService(Type serviceType)
    {
        return serviceType.IsGenericType &&
               CqrsServiceTypes.Contains(serviceType.GetGenericTypeDefinition());
    }
}
