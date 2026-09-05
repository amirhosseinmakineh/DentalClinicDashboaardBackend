using DentalDashboard.Accounting.Infrastructure.Registration;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Infrastracture.Context;
using DentalDashboard.Infrastracture.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DentalDashboard.Framwork.IRepositories;
using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.Infrastracture.Services;
namespace DentalDashboard.Infrastracture.Registration;
public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<DentalContext>(options =>
        {
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"));
            options.ConfigureWarnings(warnings =>
                warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
        });

        services.AddScoped(typeof(IBaseRepository<,>),typeof(BaseRepository<,>));
        services.AddScoped<IUserRepository,UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IUnitOfWork,UnitOfWork>();
        services.AddScoped<IUserRoleRepository, UserRoleRepository>();
        services.AddScoped<IConsultantProfileRepository, ConsultantProfileRepository>();
        services.AddScoped<IPatientProfileRepository, PatientProfileRepository>();
        services.AddScoped<ILeadAssignmentRepository, LeadAssignmentRepository>();
        services.AddScoped<ILeadAssignmentSettingRepository, LeadAssignmentSettingRepository>();
        services.AddScoped<IAttendanceRepository, AttendanceRepository>();
        services.AddScoped<IReservationRepository, ReservationRepository>();
        services.AddScoped<IUserPresenceLogRepository, UserPresenceLogRepository>();
        services.AddScoped<IPushSubscriptionRepository, PushSubscriptionRepository>();
        services.AddScoped<ISecretaryAccessService, SecretaryAccessService>();
        services.AddScoped<IServiceLogRepository, ServiceLogRepository>();
        services.AddScoped<IPatientFileRepository, PatientFileRepository>();
        services.AddAccountingInfrastructure();

        return services;
    }
}
