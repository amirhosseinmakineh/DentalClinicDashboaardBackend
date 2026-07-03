using DentalDashboard.Domain.Enums;

namespace DentalDashboard.ApplicationService.Contract.Responses.LeadResponse
{
    public record CreateConsultantPatientLeadResponse
    {
        public long LeadAssignmentId { get; set; }
        public long ConsultantProfileId { get; set; }
        public string UserName { get; set; } = default!;
        public string PhoneNumber { get; set; } = default!;
        public LeadAssignmentState LeadAssignmentState { get; set; }
        public LeadAssignmentType LeadAssignmentType { get; set; }
        public DateTime AssignedAt { get; set; }
    }
}
