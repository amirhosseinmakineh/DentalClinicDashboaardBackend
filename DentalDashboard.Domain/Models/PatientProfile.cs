namespace DentalDashboard.Domain.Models
{
    public class PatientProfile : BaseAuditableEntity<long>
    {
        public Guid UserId { get; set; }

        public string NationalCode { get; set; } = default!;

        #region Relations

        public User User { get; set; } = default!;

        #endregion
    }
}