using DentalDashboard.ApplicationService.Contract.Requests.Role;
using DentalDashboard.ApplicationService.Contract.Requests.Role.Queries;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using Microsoft.AspNetCore.Mvc;

[Route("api/[controller]")]
[ApiController]
public class RoleController : ControllerBase
{
    private readonly ICommandDispatcher dispatcher;
    private readonly IQueryDispatcher queryDispatcher;

    public RoleController(ICommandDispatcher dispatcher, IQueryDispatcher queryDispatcher)
    {
        this.dispatcher = dispatcher;
        this.queryDispatcher = queryDispatcher;
    }

    [HttpPost]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleCommand command)
    {
        var result = await dispatcher.DispatchAsync(command);
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetRoles([FromQuery] GetRolesQuery query)
    {
        var result = await queryDispatcher.DispatchAsync(query);
        return Ok(result);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateRole([FromBody] UpdateRoleCommaand command)
    {
        var result = await dispatcher.DispatchAsync(command);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRole(DeleteRoleCommaand commaand)
    {

        var result = await dispatcher.DispatchAsync(commaand);
        return Ok(result);
    }
}