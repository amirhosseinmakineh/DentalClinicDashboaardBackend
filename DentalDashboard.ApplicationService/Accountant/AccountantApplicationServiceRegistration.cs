using DentalDashboard.ApplicationService.Accountant.Services;
using DentalDashboard.ApplicationService.Contract.Accountant.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DentalDashboard.ApplicationService.Accountant;

public static class AccountantApplicationServiceRegistration
{
    public static IServiceCollection AddAccountantApplicationServices(
        this IServiceCollection services)
    {
        services.AddScoped<IFinancialTransactionReceiptService,
            FinancialTransactionReceiptService>();

        return services;
    }
}
