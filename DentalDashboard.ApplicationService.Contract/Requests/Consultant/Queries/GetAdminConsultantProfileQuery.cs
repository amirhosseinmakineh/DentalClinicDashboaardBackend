using DentalDashboard.ApplicationService.Contract.Responses.ConsultantResponse;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;

namespace DentalDashboard.ApplicationService.Contract.Requests.Consultant.Queries;

public record GetAdminConsultantProfileQuery : IQuery<AdminConsultantProfileResponse>
{
    public long ProfileId { get; init; }
}
