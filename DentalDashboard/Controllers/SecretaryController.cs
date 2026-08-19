using DentalDashboard.ApplicationService.Contract.Requests.Secretary.Commands;
using DentalDashboard.ApplicationService.Contract.Requests.Secretary.Queries;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using DentalDashboard.ApplicationService.Contract.IServices;

namespace DentalDashboard.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SecretaryController : ControllerBase
{
    private readonly ICommandDispatcher dispatcher;
    private readonly IQueryDispatcher queryDispatcher;
    private readonly ISecretaryAccessService accessService;

    public SecretaryController(ICommandDispatcher dispatcher, IQueryDispatcher queryDispatcher, ISecretaryAccessService accessService)
    {
        this.dispatcher = dispatcher;
        this.queryDispatcher = queryDispatcher;
        this.accessService = accessService;
    }

    [HttpGet("access")]
    [Authorize]
    public async Task<IActionResult> GetAccess(CancellationToken cancellationToken)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("userId") ?? User.FindFirstValue("Id");
        if (!Guid.TryParse(claim, out var userId)) return Unauthorized();
        var access = await accessService.GetAccessAsync(userId, cancellationToken);
        if (!access.IsSecretary) return Forbid();
        return Ok(new
        {
            access.IsSecretary,
            access.HasFullAccess,
            AllowedDays = access.AllowedDays.Select(x => x.ToString()),
            Permissions = access.Permissions.Select(x => x.ToString())
        });
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
        if (!await accessService.HasPermissionAsync(userId, DentalDashboard.Domain.Enums.SecretaryPermissionType.ViewReservations,
                cancellationToken)) return Forbid();
        var result = await queryDispatcher.DispatchAsync(
            new GetSecretaryDashboardSummaryQuery { SecretaryUserId = userId },
            cancellationToken);
        return Ok(result);
    }
}
