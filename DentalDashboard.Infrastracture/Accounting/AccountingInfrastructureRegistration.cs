using DentalDashboard.ApplicationService.Contract.Accounting;
using DentalDashboard.Domain.Accounting;
using DentalDashboard.Infrastracture.Accounting.Auditing;
using DentalDashboard.Infrastracture.Accounting.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace DentalDashboard.Infrastracture.Accounting;

public static class AccountingInfrastructureRegistration
{
    public static IServiceCollection AddAccountingInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IAccountingAuditScope, AccountingAuditScope>();
        services.AddScoped<AccountingAuditInterceptor>();
        services.AddScoped<IAccountingSettlementRepository, AccountingSettlementRepository>();
        services.AddScoped<DentalDashboard.Domain.Accounting.Reminders.IAccountingReminderRepository, Reminders.AccountingReminderRepository>();
        services.AddScoped<IAccountingAccessRepository, AccountingAccessRepository>();
        services.AddScoped<IAccountingReadRepository, PatientAccountingRepository>();
        services.AddScoped<IAccountingReadRepository, ClinicAccountingRepository>();
        services.AddScoped<IAccountingReadRepository, StaffAccountingRepository>();
        return services;
    }
}
