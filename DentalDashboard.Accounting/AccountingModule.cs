using DentalDashboard.Accounting.Integration.PatientFiles.Services;
using DentalDashboard.Accounting.Contracts.Services;
using DentalDashboard.Accounting.Application.Services;
using DentalDashboard.Framwork.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace DentalDashboard.Accounting;

public static class AccountingModule
{
    public static IServiceCollection AddAccountingModule(this IServiceCollection services)
    {
        services.AddScoped<IFinancialTransactionReceiptService, FinancialTransactionReceiptService>();
        services.AddScoped<IPatientFileFinanceService, PatientFileFinanceService>();

        services.AddCqrsTypesFromAssembly(typeof(AccountingModule).Assembly);

        return services;
    }
}
