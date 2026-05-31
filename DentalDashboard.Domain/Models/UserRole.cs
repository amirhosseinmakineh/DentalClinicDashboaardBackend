namespace DentalDashboard.Domain.Models
{
    public class UserRole : BaseAuditableEntity<long>
    {
        public Guid UserId { get; set; }
        public long RoleId { get; set; }
        #region Relations
        public Role Role { get; set; }
        public User User { get; set; }
        #endregion
    }
}
