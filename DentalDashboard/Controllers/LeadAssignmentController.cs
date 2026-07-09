using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.ApplicationService.Contract.Requests.Lead.Queryies;
using DentalDashboard.ApplicationService.Contract.Responses.LeadResponse;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Domain;
using Microsoft.AspNetCore.Mvc;

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
        public async Task<IActionResult> Get([FromQuery] GetAllLeadsQuery query)
        {
            var result = await dispatcher.DispatchAsync(query);
            return Ok(result);
        }

        [HttpPost("{leadAssignmentId}/pickup")]
        public async Task<IActionResult> Pickup(
            long leadAssignmentId,
            long consultantProfileId,
            CancellationToken cancellationToken)
        {
            var result = await pickupService.PickupLeadAsync(
                leadAssignmentId,
                consultantProfileId,
                cancellationToken);

            return result.Status switch
            {
                PickupLeadStatus.Success => Ok(Result<object>.Success(new
                {
                    leadAssignmentId = result.LeadAssignmentId,
                    consultantProfileId = result.ConsultantProfileId,
                    callDeadlineAt = result.CallDeadlineAt
                }, "لید با موفقیت برداشته شد")),
                PickupLeadStatus.DailyLimitReached => StatusCode(
                    StatusCodes.Status429TooManyRequests,
                    Result.Failure("سقف روزانه ۱۰ لید پر شده است. امروز دیگر نمی‌توانید لید بردارید.")),
                _ => Conflict(Result.Failure("این لید قبلاً توسط مشاور دیگری برداشته شده است."))
            };
        }
    }
}
