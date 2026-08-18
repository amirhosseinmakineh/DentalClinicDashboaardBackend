using DentalDashboard.ApplicationService.Contract.Requests.Secretary.Commands;
using DentalDashboard.ApplicationService.Contract.Requests.Secretary.Queries;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

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
    [Authorize]
    public async Task<IActionResult> GetDashboardSummary(CancellationToken cancellationToken)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("userId") ?? User.FindFirstValue("Id");
        if (!Guid.TryParse(claim, out var userId)) return Unauthorized();
        var result = await queryDispatcher.DispatchAsync(
            new GetSecretaryDashboardSummaryQuery { SecretaryUserId = userId },
            cancellationToken);
        return Ok(result);
    }
}
