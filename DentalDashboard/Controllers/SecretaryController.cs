using DentalDashboard.ApplicationService.Contract.Requests.Secretary.Commands;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using Microsoft.AspNetCore.Mvc;

namespace DentalDashboard.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SecretaryController : ControllerBase
{
    private readonly ICommandDispatcher dispatcher;

    public SecretaryController(ICommandDispatcher dispatcher)
    {
        this.dispatcher = dispatcher;
    }

    [HttpPost]
    public async Task<IActionResult> CompleteProfile(
        CompleteSecretaryProfileCommand command,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.DispatchAsync(command, cancellationToken);
        return Ok(result);
    }
}
