using DentalDashboard.Domain.Enums;

namespace DentalDashboard.Domain.Models
{
    public class User : BaseAuditableEntity<Guid>
    {
        public User()
        {
            UserRoles = new HashSet<UserRole>();
            PushSubscriptions = new HashSet<PushSubscription>();
            SecretaryAccessSchedules = new HashSet<SecretaryAccessSchedule>();
        }

        public string FirstName { get; set; } = default!;

        public string LastName { get; set; } = default!;

        public string PhoneNumber { get; set; } = default!;

        public string PasswordHash { get; set; } = default!;

        public bool IsCompleteProfile { get; set; }

        public string? AvatarImageName { get; set; }

        public Gender Gender { get; set; }

        public DateTime BirthDate { get; set; }

        public bool IsActive { get; set; } = true;

        public string? PushNotificationToken { get; set; }

        public DateTime? LastSeenAt { get; set; }

        public SecretaryType? SecretaryType { get; set; }

        #region Relations

        public PatientProfile? PatientProfile { get; set; }

        public ConsultantProfile? ConsultantProfile { get; set; }

        public ICollection<UserRole> UserRoles { get; set; }
        public ICollection<PushSubscription> PushSubscriptions { get; set; }
        public ICollection<SecretaryAccessSchedule> SecretaryAccessSchedules { get; set; }


        #endregion
    }
}
