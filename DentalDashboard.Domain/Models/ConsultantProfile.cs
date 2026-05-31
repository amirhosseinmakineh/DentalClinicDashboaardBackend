namespace DentalDashboard.Domain.Models
{
    public class ConsultantProfile : BaseAuditableEntity<long>
    {
        public ConsultantProfile()
        {
            CallAssignments = new HashSet<LeadAssignment>();
        }

        public Guid UserId { get; set; }

        public string NationalCode { get; set; } = default!;

        public string Address { get; set; } = default!;

        public bool IsAvailable { get; set; } = true;

        public TimeSpan WorkStartTime { get; set; }

        public TimeSpan WorkEndTime { get; set; }

        public string? Notes { get; set; }

        #region Relations

        public User User { get; set; } = default!;

        public ICollection<LeadAssignment> CallAssignments { get; set; }

        #endregion
    }
}