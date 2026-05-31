using DentalDashboard.Framwork.Domain;

namespace DentalDashboard.Domain.Models
{
    public abstract  class BaseAuditableEntity<TKey> : BaseEntity<TKey> where TKey : struct
    {
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public bool IsDeleted { get; set; }

        public DateTime? DeletedAt { get; set; }
    }
}
