using DentalDashboard.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Infrastracture.Context
{
    public class DentalContext : DbContext
    {
        public DentalContext(DbContextOptions<DentalContext> options):base(options)
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
        public DbSet<ReservationTimeChange> ReservationTimeChanges => Set<ReservationTimeChange>();
        public DbSet<UserNotification> UserNotifications => Set<UserNotification>();
        public DbSet<ReservationFollowUp> ReservationFollowUps => Set<ReservationFollowUp>();
        public DbSet<SecretaryReservationActivity> SecretaryReservationActivities => Set<SecretaryReservationActivity>();
        public DbSet<ReservationContactLog> ReservationContactLogs => Set<ReservationContactLog>();
        public DbSet<ReservationNote> ReservationNotes => Set<ReservationNote>();
        public DbSet<ReservationNotificationOutbox> ReservationNotificationOutbox => Set<ReservationNotificationOutbox>();


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(
                typeof(DentalContext).Assembly);
        }
    }
}
