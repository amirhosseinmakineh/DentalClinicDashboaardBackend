using DentalDashboard.ApplicationService.Contract.Responses;
using DentalDashboard.ApplicationService.Contract.Responses.LeadResponse;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;

namespace DentalDashboard.ApplicationService.Contract.Requests.Lead.Queryies
{
    public class GetLeadsByConsultantProfileId : IQuery<PaginatedResult<LeadsAssignmentItemsResponse>>
    {
        public long ConsultantProfileId { get; set; }
    }
}
