namespace DentalDashboard.Domain.Models
{
    public class LeadAssignment : BaseAuditableEntity<long>
    {
        public long ConsultantProfileId { get; set; }

        public string CustomerPhoneNumber { get; set; } = default!;

        public DateTime AssignedAt { get; set; }

        public DateTime ExpireAt { get; set; }

        public DateTime? CalledAt { get; set; }

        public bool IsCalled { get; set; }

        public bool IsExpired { get; set; }

        public bool IsPenaltyApplied { get; set; }

        public string? Notes { get; set; }

        #region Relations

        public ConsultantProfile ConsultantProfile { get; set; } = default!;

        #endregion
    }
}