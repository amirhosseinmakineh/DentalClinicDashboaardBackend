using DentalDashboard.Domain.Enums;

namespace DentalDashboard.ApplicationService.Contract.Responses.LeadResponse
{
    public record ExpireLeadNoCallResponse
    {
        public long LeadAssignmentId { get; init; }
        public long ConsultantProfileId { get; init; }
        public LeadAssignmentState LeadAssignmentState { get; init; }
        public bool IsConsultantOnline { get; init; }
        public bool WasRequeued { get; init; }
    }
}
