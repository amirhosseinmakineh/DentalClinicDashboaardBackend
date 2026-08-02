using DentalDashboard.ApplicationService.Contract.Responses.Attendance;
using DentalDashboard.ApplicationService.Contract.Responses.LeadResponse;
using DentalDashboard.Domain.Enums;

namespace DentalDashboard.ApplicationService.Contract.Responses.ConsultantResponse
{
    public record ConsultantResponse : BaseResponse<Guid>
    {
        public string FirstName { get; set; } = default!;
        public string LastName { get; set; } = default!;
        public string PhoneNumber { get; set; } = default!;
        public long ProfileId { get; set; }
        public ConsultantLevel ConsultantLevel { get; set; }
        public PaginatedResult<AttendanceResponse> AttendanceResponse { get; set; }
        public PaginatedResult<LeadsAssignmentItemsResponse> LeadsAssignmentItemsResponse { get; set; }
    }
}
