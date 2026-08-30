using DentalDashboard.Accountant.Domain.IRepositories;
using DentalDashboard.Accountant.Domain.PatientFinance.IRepositories;
using DentalDashboard.Accountant.Infrastructure.PatientFinance.Repositories;
using DentalDashboard.Accountant.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace DentalDashboard.Accountant.Infrastructure;

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
