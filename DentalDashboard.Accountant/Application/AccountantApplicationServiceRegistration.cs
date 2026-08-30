using DentalDashboard.Accountant.Application.Contracts.Services;
using DentalDashboard.Accountant.Application.Services;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace DentalDashboard.Accountant.Application;

public static class AccountantApplicationServiceRegistration
{
    public static IServiceCollection AddAccountantApplicationServices(
        this IServiceCollection services)
    {
        services.AddScoped<IFinancialTransactionReceiptService,
            FinancialTransactionReceiptService>();

        var assembly = typeof(AccountantApplicationServiceRegistration).Assembly;
        services.RegisterModuleHandlers(assembly, typeof(ICommandHandler<>));
        services.RegisterModuleHandlers(assembly, typeof(ICommandHandler<,>));
        services.RegisterModuleHandlers(assembly, typeof(IQueryHandler<,>));
        services.RegisterModuleHandlers(assembly, typeof(IValidator<>));

        return services;
    }

    private static void RegisterModuleHandlers(
        this IServiceCollection services,
        System.Reflection.Assembly assembly,
        Type openGenericHandlerType)
    {
        var registrations = assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .SelectMany(implementation => implementation.GetInterfaces()
                .Where(service => service.IsGenericType &&
                    service.GetGenericTypeDefinition() == openGenericHandlerType)
                .Select(service => new { service, implementation }));

        foreach (var registration in registrations)
            services.AddTransient(registration.service,
                registration.implementation);
    }
}
