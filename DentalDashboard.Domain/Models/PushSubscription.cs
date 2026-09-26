namespace DentalDashboard.Domain.Models
{
    public class PushSubscription : BaseAuditableEntity<long>
    {
        public Guid UserId { get; set; }

        public string Endpoint { get; set; } = null!;

        public string P256dh { get; set; } = null!;

        public string Auth { get; set; } = null!;

        #region Relations
        public User User { get; set; }
        #endregion
    }
}
