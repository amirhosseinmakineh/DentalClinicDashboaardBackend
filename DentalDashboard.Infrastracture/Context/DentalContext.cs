using DentalDashboard.Domain.Models;
using Microsoft.EntityFrameworkCore;
using DentalDashboard.Domain.Secretary.Account.Entities;

namespace DentalDashboard.Infrastracture.Context
{
    public class DentalContext : DbContext
    {
        public DentalContext(DbContextOptions<DentalContext> options) : base(options)
        {

        }
        public DbSet<User> Users => Set<User>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<UserRole> UserRoles => Set<UserRole>();
        public DbSet<PatientProfile> PatientProfiles => Set<PatientProfile>();
        public DbSet<ConsultantProfile> ConsultantProfiles => Set<ConsultantProfile>();
        public DbSet<LeadAssignment> LeadAssignments => Set<LeadAssignment>();
        public DbSet<Attendance> Attendances => Set<Attendance>();
        public DbSet<Reservation> Reservations => Set<Reservation>();
        public DbSet<UserPresenceLog> UserPresenceLogs => Set<UserPresenceLog>();
        public DbSet<PushSubscription> PushSubscriptions => Set<PushSubscription>();
        public DbSet<SecretaryAccessSchedule> SecretaryAccessSchedules => Set<SecretaryAccessSchedule>();
        public DbSet<SecretaryAccessScheduleAudit> SecretaryAccessScheduleAudits => Set<SecretaryAccessScheduleAudit>();
        public DbSet<SecretaryAccessPermission> SecretaryAccessPermissions => Set<SecretaryAccessPermission>();
        public DbSet<ServiceLog> ServiceLogs => Set<ServiceLog>();
        public DbSet<FinancialTransaction> FinancialTransactions => Set<FinancialTransaction>();
        public DbSet<ExpenseCategory> ExpenseCategories => Set<ExpenseCategory>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(
                typeof(DentalContext).Assembly);
        }
    }
}
