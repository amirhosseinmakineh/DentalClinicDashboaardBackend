using DentalDashboard.ApplicationService.Contract.Requests.Auth;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
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
        [Authorize]
        [HttpGet("Me")]
        public IActionResult Me()
        {
            var roles = User.FindAll(ClaimTypes.Role)
                .Select(x => x.Value)
                .Distinct()
                .ToList();

            var primaryRole = ResolvePrimaryRole(roles);

            return Ok(new
            {
                IsAuthenticated = User.Identity?.IsAuthenticated == true,
                UserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("userId") ?? User.FindFirstValue("Id"),
                FirstName = User.FindFirstValue(ClaimTypes.GivenName) ?? User.FindFirstValue("firstName") ?? User.FindFirstValue("FirstName"),
                LastName = User.FindFirstValue(ClaimTypes.Surname) ?? User.FindFirstValue("lastName") ?? User.FindFirstValue("LastName"),
                FullName = User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue("fullName") ?? User.FindFirstValue("FullName"),
                PhoneNumber = User.FindFirstValue("phoneNumber") ?? User.FindFirstValue("PhoneNumber"),
                Role = primaryRole,
                Roles = roles,
                DefaultDashboard = ResolveDashboardKey(primaryRole),
                DefaultDashboardRoute = ResolveDashboardRoute(primaryRole),
                DashboardAccess = roles.Select(role => new
                {
                    Role = role,
                    Dashboard = ResolveDashboardKey(role),
                    Route = ResolveDashboardRoute(role)
                }).ToList()
            });
        }

        private static string ResolvePrimaryRole(IReadOnlyCollection<string> roles)
        {
            if (roles.Contains("Admin"))
                return "Admin";

            if (roles.Contains("Consultant"))
                return "Consultant";

            if (roles.Contains("Patient"))
                return "Patient";

            if (roles.Contains("NormalUser"))
                return "NormalUser";

            if (roles.Contains("User"))
                return "User";

            return roles.FirstOrDefault() ?? string.Empty;
        }

        private static string ResolveDashboardKey(string role)
        {
            return role switch
            {
                "Admin" => "AdminDashboard",
                "Consultant" => "ConsultantDashboard",
                "Patient" => "PatientDashboard",
                "NormalUser" => "UserDashboard",
                "User" => "UserDashboard",
                _ => "UserDashboard"
            };
        }

        private static string ResolveDashboardRoute(string role)
        {
            return role switch
            {
                "Admin" => "/admin/dashboard",
                "Consultant" => "/consultant/dashboard",
                "Patient" => "/patient/dashboard",
                "NormalUser" => "/dashboard",
                "User" => "/dashboard",
                _ => "/dashboard"
            };
        }

    }
}
