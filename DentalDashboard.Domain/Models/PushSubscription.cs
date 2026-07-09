using System;
using System.Collections.Generic;
using System.Text;

namespace DentalDashboard.Domain.Models
{
    public class PushSubscription : BaseAuditableEntity<long>
    {
        public Guid UserId { get; set; }

        public string Endpoint { get; set; } = null!;

        public string P256dh { get; set; } = null!;

        public string Auth { get; set; } = null!;

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public bool IsDeleted { get; set; }
        #region Relations
        public User User { get; set; }
        #endregion
    }
}
