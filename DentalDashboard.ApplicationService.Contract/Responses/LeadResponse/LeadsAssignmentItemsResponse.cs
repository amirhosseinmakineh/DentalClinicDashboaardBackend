using DentalDashboard.Domain.Enums;

namespace DentalDashboard.ApplicationService.Contract.Responses.LeadResponse
{
    public record LeadsAssignmentItemsResponse : BaseResponse<long>
    {
        public string UserName { get; set; } = default!;
        public string PhoneNumber { get; set; } = default!;
        public LeadAssignmentState LeadAssignmentState { get; set; }
        public LeadAssignmentType leadAssignmentType { get; set; }
        public bool HasActiveReservation { get; set; }
        public DateTime? AssignedAt { get; set; }
        public DateTime? CallDeadlineAt { get; set; }
        public bool RequiresThreeMinuteCall { get; set; }
        public bool IsReportSubmitted { get; set; }
        public DateTime? ReportSubmittedAt { get; set; }
        public DateTime? ContactedAt { get; set; }
        public DateTime? CallInitiatedAt { get; set; }
        public LeadCallResult? CallResult { get; set; }
        public string? ReportDescription { get; set; }
        public string? PatientCity { get; set; }
        public string? PatientRegion { get; set; }
        public string? BusinessName { get; set; }
        public int? AttendanceProbabilityPercent { get; set; }
        public string? SecondaryPhoneNumber { get; set; }
    }
}
