using DentalDashboard.ApplicationService.Contract.Responses;
using DentalDashboard.ApplicationService.Contract.Responses.ConsultantResponse;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using System;
using System.Collections.Generic;
using System.Text;

namespace DentalDashboard.ApplicationService.Contract.Requests.Consultant
{
    public class GetLeadsQuery : IQuery<PaginatedResult<LeadsAssignmentItemsResponse>>
    {
        public long  ProfileId { get; set; }
        public LeadAssignmentState? leadAssignmentState { get; set; }
        public LeadAssignmentType? LeadAssignmentType { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
