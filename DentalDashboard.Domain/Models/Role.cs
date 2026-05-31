namespace DentalDashboard.Domain.Models
{
    public class Role : BaseAuditableEntity<long>
    {
        public Role()
        {
            UserRoles = new HashSet<UserRole>();
        }
        public string RoleName { get; set; } = string.Empty;
        #region Relaations
        public ICollection<UserRole> UserRoles { get; set; }
        #endregion
    }
}
