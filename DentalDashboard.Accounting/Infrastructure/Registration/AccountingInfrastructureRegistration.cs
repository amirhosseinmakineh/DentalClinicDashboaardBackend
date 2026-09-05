using DentalDashboard.Accounting.Domain.IRepositories;
using DentalDashboard.Accounting.Domain.PatientFinance.IRepositories;
using DentalDashboard.Accounting.Domain.SecretarySales.IRepositories;
using DentalDashboard.Accounting.Infrastructure.PatientFinance.Repositories;
using DentalDashboard.Accounting.Infrastructure.Repositories;
using DentalDashboard.Accounting.Infrastructure.SecretarySales.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace DentalDashboard.Accounting.Infrastructure.Registration;

public static class AccountingInfrastructureRegistration
{
    public static IServiceCollection AddAccountingInfrastructure(
        this IServiceCollection services)
    {
        services.AddScoped<ISecretaryAccountRepository, SecretaryAccountRepository>();
        services.AddScoped<IExpenseRepository, ExpenseRepository>();
        services.AddScoped<IPatientFinanceRepository, PatientFinanceRepository>();
        services.AddScoped<ISecretarySalesRepository, SecretarySalesRepository>();

        return services;
    }
}
