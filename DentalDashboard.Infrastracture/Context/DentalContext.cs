using DentalDashboard.Domain.Models;
using Microsoft.EntityFrameworkCore;
using DentalDashboard.Domain.Secretary.Accountant.Entities;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Entities;
using DentalDashboard.Domain.Secretary.Accountant.SecretarySales.Entities;

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
        public DbSet<LeadAssignmentSetting> LeadAssignmentSettings => Set<LeadAssignmentSetting>();
        public DbSet<LeadAssignmentHistory> LeadAssignmentHistories => Set<LeadAssignmentHistory>();
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
        public DbSet<PatientFinancialCase> PatientFinancialCases => Set<PatientFinancialCase>();
        public DbSet<PatientCheque> PatientCheques => Set<PatientCheque>();
        public DbSet<PatientPromissoryNote> PatientPromissoryNotes => Set<PatientPromissoryNote>();
        public DbSet<PatientDebt> PatientDebts => Set<PatientDebt>();
        public DbSet<PatientFinancialTransaction> PatientFinancialTransactions => Set<PatientFinancialTransaction>();
        public DbSet<PatientFile> PatientFiles => Set<PatientFile>();
        public DbSet<SecretarySaleService> SecretarySaleServices => Set<SecretarySaleService>();
        public DbSet<SecretarySale> SecretarySales => Set<SecretarySale>();
        public DbSet<SecretaryWallet> SecretaryWallets => Set<SecretaryWallet>();
        public DbSet<SecretaryWalletTransaction> SecretaryWalletTransactions => Set<SecretaryWalletTransaction>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(
                typeof(DentalContext).Assembly);
        }
    }
}
