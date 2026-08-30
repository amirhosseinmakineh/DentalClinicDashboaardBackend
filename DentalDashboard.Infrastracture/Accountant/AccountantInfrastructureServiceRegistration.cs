using DentalDashboard.Domain.Accountant.IRepositories;
using DentalDashboard.Domain.Accountant.PatientFinance.IRepositories;
using DentalDashboard.Infrastracture.Accountant.PatientFinance.Repositories;
using DentalDashboard.Infrastracture.Accountant.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace DentalDashboard.Infrastracture.Accountant;

public static class AccountantInfrastructureServiceRegistration
{
    public static IServiceCollection AddAccountantInfrastructure(
        this IServiceCollection services)
    {
        services.AddScoped<IAccountantRepository,
            AccountantRepository>();
        services.AddScoped<IExpenseRepository, ExpenseRepository>();
        services.AddScoped<IPatientFinanceRepository,
            PatientFinanceRepository>();

        return services;
    }
}
