using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.Models;

namespace DentalDashboard.ApplicationService.Contract.Dtos.LeadAssignment
{
    public record LeadAssignmentDto
    {
        public string UserName { get; set; } = default!;
        public string PhoneNumber { get; set; } = default!;
        public DateTime CreateAt { get; set; }
        public LeadAssignmentState LeadAssignmentState { get; set; }
    }
}
