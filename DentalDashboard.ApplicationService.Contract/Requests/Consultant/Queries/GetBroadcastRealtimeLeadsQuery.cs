using DentalDashboard.ApplicationService.Contract.Responses.ConsultantResponse;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;

namespace DentalDashboard.ApplicationService.Contract.Requests.Consultant.Queries;

public record GetBroadcastRealtimeLeadsQuery : IQuery<BroadcastRealtimeLeadsResponse>
{
    public long ProfileId { get; set; }
}
