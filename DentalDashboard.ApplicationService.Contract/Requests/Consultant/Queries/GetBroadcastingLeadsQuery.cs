using DentalDashboard.ApplicationService.Contract.Responses;
using DentalDashboard.ApplicationService.Contract.Responses.LeadResponse;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;

namespace DentalDashboard.ApplicationService.Contract.Requests.Consultant.Queries;

public class GetBroadcastingLeadsQuery : IQuery<PaginatedResult<BroadcastingLeadResponse>>
{
    public long ProfileId { get; set; }
}
