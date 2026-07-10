using DentalDashboard.ApplicationService.Contract.Requests.Consultant.Commands;
using DentalDashboard.ApplicationService.Contract.Requests.Consultant.Queries;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalDashboard.Controllers;

[Route("api/admin/consultants")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminConsultantsController : ControllerBase
{
    private readonly IQueryDispatcher queryDispatcher;
    private readonly ICommandDispatcher commandDispatcher;

    public AdminConsultantsController(
        IQueryDispatcher queryDispatcher,
        ICommandDispatcher commandDispatcher)
    {
        this.queryDispatcher = queryDispatcher;
        this.commandDispatcher = commandDispatcher;
    }

    [HttpGet("{profileId:long}")]
    public async Task<IActionResult> GetProfile(
        long profileId,
        CancellationToken cancellationToken)
    {
        try
        {
            var query = new GetAdminConsultantProfileQuery { ProfileId = profileId };
            var result = await queryDispatcher.DispatchAsync(query, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPatch("{profileId:long}/limit")]
    public async Task<IActionResult> UpdateLimit(
        long profileId,
        [FromBody] UpdateConsultantLimitNumberCommand command,
        CancellationToken cancellationToken)
    {
        command.ProfileId = profileId;
        var result = await commandDispatcher.DispatchAsync(command, cancellationToken);
        return Ok(result);
    }
}
