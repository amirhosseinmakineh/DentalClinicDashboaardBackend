using DentalDashboard.ApplicationService.Contract.Requests.Auth;
using DentalDashboard.ApplicationService.Contract.Responses.AuthResponse;
using DentalDashboard.ApplicationService.Handlers.CommandHandlers.Auth.Helpers;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
        public async Task<IActionResult> Register(RegisterCommand command, CancellationToken cancellationToken)
        {
            var result = await dispatcher.DispatchAsync(command, cancellationToken);
            return Ok(result);
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login(LoginCommand command, CancellationToken cancellationToken)
        {
            var result = await dispatcher.DispatchAsync(command, cancellationToken);
            return Ok(result);
        }

        [Authorize]
        [HttpGet("Me")]
        public IActionResult Me()
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return Ok(Result<AuthenticatedUserResponse>.Failure("کاربر احراز هویت نشده است"));
            }

            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                              User.FindFirstValue("userId") ??
                              User.FindFirstValue("Id");

            if (!Guid.TryParse(userIdValue, out var userId))
            {
                return Ok(Result<AuthenticatedUserResponse>.Failure("شناسه کاربر در توکن معتبر نیست"));
            }

            var roles = User.FindAll(ClaimTypes.Role)
                .Select(x => x.Value)
                .Distinct()
                .ToList();

            var response = AuthResponseFactory.CreateAuthenticatedUserResponse(
                userId,
                User.FindFirstValue(ClaimTypes.GivenName) ?? User.FindFirstValue("firstName") ?? User.FindFirstValue("FirstName") ?? string.Empty,
                User.FindFirstValue(ClaimTypes.Surname) ?? User.FindFirstValue("lastName") ?? User.FindFirstValue("LastName") ?? string.Empty,
                User.FindFirstValue("phoneNumber") ?? User.FindFirstValue("PhoneNumber") ?? string.Empty,
                roles);

            return Ok(Result<AuthenticatedUserResponse>.Success(response));
        }
    }
}
