using DentalDashboard.ApplicationService.Contract.Common;
using DentalDashboard.ApplicationService.Contract.Dtos.User;
using DentalDashboard.ApplicationService.Contract.Requests.User.Commands.CreateUser;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using Microsoft.AspNetCore.Mvc;

namespace DentalClinic.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly ICommandDispatcher dispatcher;

    public UserController(ICommandDispatcher dispatcher)
    {
        this.dispatcher = dispatcher;
    }
    [HttpPost]
    public IActionResult CreateUser(CreateUserCommand command)
    {
        return null;
    }
}