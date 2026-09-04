using DentalDashboard.Domain.Secretary.Accountant.IRepositories;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.IRepositories;
using DentalDashboard.Domain.Secretary.Accountant.SecretarySales.IRepositories;
using DentalDashboard.Infrastracture.Secretary.Accountant.PatientFinance.Repositories;
using DentalDashboard.Infrastracture.Secretary.Accountant.Repositories;
using DentalDashboard.Infrastracture.Secretary.Accountant.SecretarySales.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace DentalDashboard.Infrastracture.Registration;

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
