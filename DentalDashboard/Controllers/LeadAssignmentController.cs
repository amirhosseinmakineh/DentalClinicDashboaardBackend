using DentalDashboard.ApplicationService.Contract.Requests.Lead.Queryies;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DentalDashboard.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LeadAssignmentController : ControllerBase
    {
        private readonly IQueryDispatcher dispatcher;

        public LeadAssignmentController(IQueryDispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
        }

        [HttpGet]
        public IActionResult Get([FromQuery]GetAllLeadsQuery query)
        {
            var result = dispatcher.DispatchAsync(query);
            return Ok(result);
        }
    }
}
