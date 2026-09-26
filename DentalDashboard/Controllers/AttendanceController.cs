using DentalDashboard.ApplicationService.Contract.Requests.Attendance.Queryies;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using DentalDashboard.Domain.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,Consultant")]
    public class AttendanceController : ControllerBase
    {
        private readonly IQueryDispatcher dispatcher;
        private readonly IConsultantProfileRepository consultantProfiles;

        public AttendanceController(
            IQueryDispatcher dispatcher,
            IConsultantProfileRepository consultantProfiles)
        {
            this.dispatcher = dispatcher;
            this.consultantProfiles = consultantProfiles;
        }

        [HttpGet]
        public async Task<IActionResult> GetAttendances(
            [FromQuery] GetAttendancesQuery query,
            CancellationToken cancellationToken)
        {
            if (!User.IsInRole("Admin"))
            {
                var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                            User.FindFirstValue("userId") ??
                            User.FindFirstValue("Id");
                if (!Guid.TryParse(claim, out var userId)) return Unauthorized();

                var profileId = await consultantProfiles.GetAll()
                    .AsNoTracking()
                    .Where(x => !x.IsDeleted && x.UserId == userId)
                    .Select(x => (long?)x.Id)
                    .FirstOrDefaultAsync(cancellationToken);
                if (!profileId.HasValue) return Forbid();
                query.ConsultantProfileId = profileId.Value;
            }

            var result = await dispatcher.DispatchAsync(query, cancellationToken);
            return Ok(result);
        }
    }
}
