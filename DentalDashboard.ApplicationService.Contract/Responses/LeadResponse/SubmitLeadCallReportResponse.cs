using DentalDashboard.Domain.Enums;

namespace DentalDashboard.ApplicationService.Contract.Responses.LeadResponse
{
    public record SubmitLeadCallReportResponse
    {
        public long LeadAssignmentId { get; init; }
        public long ConsultantProfileId { get; init; }
        public bool IsReportSubmitted { get; init; }
        public DateTime ReportSubmittedAt { get; init; }
        public LeadAssignmentState LeadAssignmentState { get; init; }
        public LeadCallResult CallResult { get; init; }
        public bool IsConsultantOnline { get; init; }
        public bool ShouldOpenReservationPage { get; init; }
    }
}
