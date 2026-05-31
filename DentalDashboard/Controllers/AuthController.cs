using DentalDashboard.ApplicationService.Contract.Requests.Auth;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DentalDashboard.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ICommandDispatcher dispatcher;

        public AuthController(ICommandDispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterCommand command)
        {
            var result = await dispatcher.DispatchAsync(command);
            return Ok(result);
        }
        [HttpPost("Login")]
        public async Task<IActionResult> Login(LoginCommand command)
        {
            var result = await dispatcher.DispatchAsync(command);
            return Ok(result);
        }
    }
}
