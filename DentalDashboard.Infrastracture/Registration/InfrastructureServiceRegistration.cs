using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Infrastracture.Context;
using DentalDashboard.Infrastracture.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DentalDashboard.Framwork.IRepositories;
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
        });

        services.AddScoped(typeof(IBaseRepository<,>),typeof(BaseRepository<,>));
        services.AddScoped<IUserRepository,UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IUnitOfWork,UnitOfWork>();
        services.AddScoped<IUserRoleRepository, UserRoleRepository>();
        services.AddScoped<IConsultantProfileRepository, ConsultantProfileRepository>();
        services.AddScoped<IPatientProfileRepository, PatientProfileRepository>();
        services.AddScoped<ILeadAssignmentRepository, LeadAssignmentRepository>();
        services.AddScoped<IAttendanceRepository, AttendanceRepository>();
        services.AddScoped<IScoreLogRepository, ScoreLogRepository>();


        return services;
    }
}