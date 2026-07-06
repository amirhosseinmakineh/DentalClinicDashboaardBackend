using DentalDashboard.ApplicationService.Contract.Requests.Presence.Queries;
using DentalDashboard.ApplicationService.Contract.Responses;
using DentalDashboard.ApplicationService.Contract.Responses.Presence;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using Microsoft.AspNetCore.Mvc;

namespace DentalDashboard.Controllers;

[Route("api/admin/presence")]
[ApiController]
public class AdminPresenceController : ControllerBase
{
    private readonly IQueryDispatcher dispatcher;

    public AdminPresenceController(IQueryDispatcher dispatcher)
    {
        this.dispatcher = dispatcher;
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview(
        [FromQuery] DateOnly date,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? firstName = null,
        [FromQuery] string? lastName = null,
        [FromQuery] string? phoneNumber = null,
        [FromQuery] string? roleName = null,
        [FromQuery] bool? isCurrentlyOnline = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetUserPresenceOverviewQuery
        {
            Date = date,
            PageNumber = pageNumber,
            PageSize = pageSize,
            FirstName = firstName,
            LastName = lastName,
            PhoneNumber = phoneNumber,
            RoleName = roleName,
            IsCurrentlyOnline = isCurrentlyOnline
        };

        var result = await dispatcher.DispatchAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("events")]
    public async Task<IActionResult> GetEvents(
        [FromQuery] DateOnly date,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] Guid? userId = null,
        [FromQuery] string? search = null,
        [FromQuery] int? eventType = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetUserPresenceEventsQuery
        {
            Date = date,
            PageNumber = pageNumber,
            PageSize = pageSize,
            UserId = userId,
            Search = search,
            EventType = eventType.HasValue
                ? (Domain.Enums.UserPresenceEventType)eventType.Value
                : null
        };

        var result = await dispatcher.DispatchAsync(query, cancellationToken);
        return Ok(result);
    }
}
