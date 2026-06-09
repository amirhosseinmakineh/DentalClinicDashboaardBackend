using DentalDashboard.ApplicationService.Contract.Responses;
using DentalDashboard.ApplicationService.Contract.Responses.ConsultantResponse;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;

namespace DentalDashboard.ApplicationService.Contract.Requests.Consultant.Queries
{
    public class GetLeadsQuery : IQuery<PaginatedResult<LeadsAssignmentItemsResponse>>
    {
        public long ProfileId { get; set; }
        public LeadAssignmentState? leadAssignmentState { get; set; }
        public LeadAssignmentType? LeadAssignmentType { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
    public record GetConsultantQuery : IQuery<PaginatedResult<ConsultantResponse>>
    {
        public string FirstName { get; set; } = default!;
        public string LastName { get; set; } = default!;
        public string PhoneNumber { get; set; } = default!;
        public long ProfileId { get; set; }
        public DateTime? AttendanceDate { get; set; }
        public TimeOnly? CheckInTime { get; set; }
        public TimeOnly? CheckOutTime { get; set; }
        public AttendanceStatus? Status { get; set; }
        public string? Description { get; set; }
        public ScoreType? ScoreType { get; set; }
        public int ScoreValue { get; set; }
        public string? ScoreValueDescription { get; set; }
        public LeadAssignmentState? LeadAssignmentState { get; set; }
        public LeadAssignmentType? leadAssignmentType { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
