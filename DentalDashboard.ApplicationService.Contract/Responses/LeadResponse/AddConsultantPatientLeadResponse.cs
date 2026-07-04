using DentalDashboard.Domain.Enums;

namespace DentalDashboard.ApplicationService.Contract.Responses.LeadResponse
{
    public class AddConsultantPatientLeadResponse
    {
        public long LeadAssignmentId { get; set; }
        public long ConsultantProfileId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public LeadAssignmentType AssignmentType { get; set; }
        public LeadAssignmentState LeadAssignmentState { get; set; }
    }
}
