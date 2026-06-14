using DentalDashboard.Domain.Enums;
using DentalDashboard.Framwork.Domain;

namespace DentalDashboard.ApplicationService.Contract.Common
{
    public class UserFilter 
    {
        public string? Search { get; set; }

        public UserRole? Role { get; set; }

        public bool? IsActive { get; set; }

    }
}
