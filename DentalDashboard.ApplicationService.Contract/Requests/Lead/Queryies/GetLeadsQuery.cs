using DentalDashboard.ApplicationService.Contract.Responses;
using DentalDashboard.ApplicationService.Contract.Responses.LeadResponse;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;

namespace DentalDashboard.ApplicationService.Contract.Requests.Lead.Queryies
{
    public class GetLeadsQuery : IQuery<PaginatedResult<LeadsAssignmentItemsResponse>>
    {
        public long ProfileId { get; set; }
        public LeadAssignmentState? leadAssignmentState { get; set; }
        public LeadAssignmentType? LeadAssignmentType { get; set; }
        public bool? HasSubmittedReport { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public LeadActivityFilter? LeadActivityFilter { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
    public class GetAllLeadsQuery : IQuery<PaginatedResult<LeadsAssignmentItemsResponse>>
    {
        public LeadAssignmentState? leadAssignmentState { get; set; }
        public LeadAssignmentType? LeadAssignmentType { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
