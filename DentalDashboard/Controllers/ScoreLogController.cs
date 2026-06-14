using DentalDashboard.ApplicationService.Contract.Requests.ScoreLog.Commands;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DentalDashboard.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ScoreLogController : ControllerBase
    {
        private readonly ICommandDispatcher dispatcher;

        public ScoreLogController(ICommandDispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
        }
        [HttpPost]
        public async Task<IActionResult> SetAdminScore(ScoreLogCommand command)
        {
            var result = await dispatcher.DispatchAsync(command);
            return Ok(result);

        }
    }
}
