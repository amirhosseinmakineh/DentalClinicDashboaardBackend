using DentalDashboard.Domain.Enums;

namespace DentalDashboard.ApplicationService.Contract.Responses.LeadResponse
{
    public record LeadsAssignmentItemsResponse : BaseResponse<long>
    {
        public string UserName { get; set; } = default!;
        public string PhoneNumber { get; set; } = default!;
        public LeadAssignmentState LeadAssignmentState { get; set; }
        public LeadAssignmentType leadAssignmentType { get; set; }
    }
}
