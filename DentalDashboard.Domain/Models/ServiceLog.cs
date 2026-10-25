namespace DentalDashboard.Domain.Models
{
    public class ServiceLog : BaseAuditableEntity<long>
    {
        public string ResponseLog { get; set; } = string.Empty;
        public string LogName { get; set; } = string.Empty;
    }
}
