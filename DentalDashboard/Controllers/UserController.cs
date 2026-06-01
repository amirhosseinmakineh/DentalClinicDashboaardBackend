using DentalDashboard.ApplicationService.Contract.Common;
using DentalDashboard.ApplicationService.Contract.Dtos.User;
using DentalDashboard.ApplicationService.Contract.Requests.User.Commands.CreateUser;
using DentalDashboard.ApplicationService.Contract.Requests.User.Commands.DeleteUser;
using DentalDashboard.ApplicationService.Contract.Requests.User.Commands.UpddateUser;
using DentalDashboard.ApplicationService.Contract.Requests.User.Queries.User;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly ICommandDispatcher dispatcher;
    private readonly IQueryDispatcher queryDispatcher;
    public UserController(ICommandDispatcher dispatcher, IQueryDispatcher queryDispatcher)
    {
        this.dispatcher = dispatcher;
        this.queryDispatcher = queryDispatcher;
    }
    [HttpPost]
    public IActionResult CreateUser(CreateUserCommand command)
    {
        var result = dispatcher.DispatchAsync(command);
        return Ok(result);
    }
    [HttpGet]
    public async Task<IActionResult> GetUsers([FromQuery] GetUsersQuery query,CancellationToken cancellationToken)
    {
        var result = await queryDispatcher.DispatchAsync(query);

        return Ok(result);
    }
    [HttpPatch]
    public IActionResult UpdateUser(UpdateUserCommand command) 
    {
        var result = dispatcher.DispatchAsync(command);
        return Ok(result);
    }
    [HttpDelete]
    public IActionResult DeleteUser(DeleteUserCommand command)
    {
        var result = dispatcher.DispatchAsync(command);
        return Ok(result);
    }
}