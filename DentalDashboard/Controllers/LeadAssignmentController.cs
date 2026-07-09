using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.ApplicationService.Contract.Requests.Lead.Queryies;
using DentalDashboard.Domain.Models;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DentalDashboard.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LeadAssignmentController : ControllerBase
    {
        private readonly IQueryDispatcher dispatcher;
        private readonly IPickupService pickupService;
        public LeadAssignmentController(IQueryDispatcher dispatcher, IPickupService pickupService)
        {
            this.dispatcher = dispatcher;
            this.pickupService = pickupService;
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery]GetAllLeadsQuery query)
        {
            var result = await dispatcher.DispatchAsync(query);
            return Ok(result);
        }
        [HttpPost("{leadAssignmentId}/pickup")]
        public async Task<IActionResult> Pickup (long leadAssignmentId,long consultantProfileId,CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var result = await pickupService.PickupLeadAsync(leadAssignmentId, consultantProfileId, cancellationToken);

            if (!result)
            {
                return Conflict(
                    "Lead has already been picked up.");
            }

            return Ok();
        }
    }
}
