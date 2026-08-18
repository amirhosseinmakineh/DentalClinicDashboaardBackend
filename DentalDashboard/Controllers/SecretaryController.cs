using DentalDashboard.ApplicationService.Contract.Requests.Secretary.Commands;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.ApplicationService.Contract.Requests.Reservation.Queries;
using Microsoft.AspNetCore.Mvc;

namespace DentalDashboard.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SecretaryController : ControllerBase
{
    private readonly ICommandDispatcher dispatcher;
    private readonly IQueryDispatcher queryDispatcher;

    public SecretaryController(ICommandDispatcher dispatcher, IQueryDispatcher queryDispatcher)
    {
        this.dispatcher = dispatcher;
        this.queryDispatcher = queryDispatcher;
    }

    [HttpPost]
    public async Task<IActionResult> CompleteProfile(
        CompleteSecretaryProfileCommand command,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.DispatchAsync(command, cancellationToken);
        return Ok(result);
    }

    [HttpGet("dashboard/summary")]
    public async Task<IActionResult> GetDashboardSummary(CancellationToken cancellationToken)
    {
        var result = await queryDispatcher.DispatchAsync(new GetSecretaryDashboardSummaryQuery(), cancellationToken);
        return Ok(result);
    }
}
