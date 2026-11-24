using DentalDashboard.ApplicationService.Accounting.Services;
using DentalDashboard.ApplicationService.Contract.Accounting;
using Microsoft.Extensions.DependencyInjection;

namespace DentalDashboard.ApplicationService.Accounting;

public static class AccountingServiceRegistration
{
    public static IServiceCollection AddAccountingServices(this IServiceCollection services)
    {
        services.AddScoped<IAccountingAccess, AccountingAccessService>();
        services.AddScoped<AccountingQueryService>();
        services.AddScoped<AccountingOperationService>();
        services.AddScoped<AccountingPermissionService>();
        services.AddScoped<AccountingSettlementService>();
        services.AddScoped<AccountingExportService>();
        services.AddScoped<AccountingDocumentService>();
        services.AddScoped<IAccountingOperationExecutor, PatientAccountingOperations>();
        services.AddScoped<IAccountingOperationExecutor, ClinicAccountingOperations>();
        services.AddScoped<IAccountingOperationExecutor, RewardAccountingOperations>();
        return services;
    }
}
