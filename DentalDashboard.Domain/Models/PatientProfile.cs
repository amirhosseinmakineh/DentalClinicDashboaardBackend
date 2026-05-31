namespace DentalDashboard.Domain.Models
{
    public class PatientProfile : BaseAuditableEntity<long>
    {
        public Guid UserId { get; set; }

        public string NationalCode { get; set; } = default!;

        public string Address { get; set; } = default!;

        public string? EmergencyPhoneNumber { get; set; }

        public string? InsuranceName { get; set; }

        public string? Notes { get; set; }

        #region Relations

        public User User { get; set; } = default!;

        #endregion
    }
}