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
        private readonly ILeadAssignmentLimitService leadAssignmentLimitService;

        public LeadAssignmentController(
            IQueryDispatcher dispatcher,
            IPickupService pickupService,
            ILeadAssignmentLimitService leadAssignmentLimitService)
        {
            this.dispatcher = dispatcher;
            this.pickupService = pickupService;
            this.leadAssignmentLimitService = leadAssignmentLimitService;
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

            if (result.Status == PickupLeadStatus.Success)
            {
                return Ok(Result<object>.Success(new
                {
                    leadAssignmentId = result.LeadAssignmentId,
                    consultantProfileId = result.ConsultantProfileId,
                    callDeadlineAt = result.CallDeadlineAt
                }, "لید با موفقیت برداشته شد"));
            }

            if (result.Status == PickupLeadStatus.DailyLimitReached)
            {
                var limitStatus = await leadAssignmentLimitService
                    .GetDailyLimitStatusAsync(consultantProfileId);

                return StatusCode(
                    StatusCodes.Status429TooManyRequests,
                    Result.Failure(limitStatus.DailyLimitReachedMessage));
            }

            return Conflict(Result.Failure("این لید قبلاً توسط مشاور دیگری برداشته شده است."));
        }
    }
}
