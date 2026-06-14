using DentalDashboard.ApplicationService.Contract.Requests.Attendance.Queryies;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DentalDashboard.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AttendanceController : ControllerBase
    {
        private readonly IQueryDispatcher dispatcher;

        public AttendanceController(IQueryDispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
        }

        [HttpGet]
        public async Task<IActionResult> GetAttendances([FromQuery]GetAttendancesQuery query)
        {
            var result = await dispatcher.DispatchAsync(query);
            return Ok(result);

        }
    }
}
