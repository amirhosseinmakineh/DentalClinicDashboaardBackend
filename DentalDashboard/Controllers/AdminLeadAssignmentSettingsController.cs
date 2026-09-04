using System.Security.Claims;
using DentalDashboard.ApplicationService.Contract.Requests.Admin.LeadAssignmentSettings;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalDashboard.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/lead-assignment-settings")]
public sealed class AdminLeadAssignmentSettingsController(
    IQueryDispatcher queries,
    ICommandDispatcher commands) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken) =>
        Ok(await queries.DispatchAsync(new GetLeadAssignmentSettingQuery(), cancellationToken));

    [HttpPut]
    public async Task<IActionResult> Update(
        UpdateLeadAssignmentSettingCommand command,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                User.FindFirstValue("userId") ??
                User.FindFirstValue("Id"),
                out var adminUserId))
            return Unauthorized();

        command.AdminUserId = adminUserId;
        var result = await commands.DispatchAsync(command, cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }
}
