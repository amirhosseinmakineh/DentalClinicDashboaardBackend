using DentalDashboard.Domain.Enums;

namespace DentalDashboard.ApplicationService.Contract.IServices
{
    public record ExpireLeadRequeueResult
    {
        public long LeadAssignmentId { get; init; }
        public long ConsultantProfileId { get; init; }
        public LeadAssignmentState LeadAssignmentState { get; init; }
        public int DeductedScore { get; init; }
        public int EventScore { get; init; }
        public int CurrentScore { get; init; }
        public bool IsConsultantOnline { get; init; }
        public bool WasRequeued { get; init; }
    }
}
