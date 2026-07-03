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
    }
}
