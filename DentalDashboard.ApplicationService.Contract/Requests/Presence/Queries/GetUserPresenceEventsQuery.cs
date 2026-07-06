using DentalDashboard.ApplicationService.Contract.Responses;
using DentalDashboard.ApplicationService.Contract.Responses.Presence;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;

namespace DentalDashboard.ApplicationService.Contract.Requests.Presence.Queries;

public record GetUserPresenceEventsQuery : IQuery<PaginatedResult<UserPresenceEventItemResponse>>
{
    public DateOnly Date { get; init; }

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 10;

    public Guid? UserId { get; init; }

    public string? Search { get; init; }

    public UserPresenceEventType? EventType { get; init; }
}
