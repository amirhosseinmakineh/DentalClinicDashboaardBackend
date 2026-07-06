using DentalDashboard.ApplicationService.Contract.Responses;
using DentalDashboard.ApplicationService.Contract.Responses.Presence;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;

namespace DentalDashboard.ApplicationService.Contract.Requests.Presence.Queries;

public record GetUserPresenceOverviewQuery : IQuery<PaginatedResult<UserPresenceOverviewItemResponse>>
{
    public DateOnly Date { get; init; }

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 10;

    public string? FirstName { get; init; }

    public string? LastName { get; init; }

    public string? PhoneNumber { get; init; }

    public string? RoleName { get; init; }

    public bool? IsCurrentlyOnline { get; init; }
}
