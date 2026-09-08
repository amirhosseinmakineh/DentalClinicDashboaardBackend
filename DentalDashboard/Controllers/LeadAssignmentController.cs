using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.ApplicationService.Contract.Requests.Lead.Queryies;
using DentalDashboard.ApplicationService.Contract.Responses.LeadResponse;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LeadAssignmentController : ControllerBase
    {
        private readonly IQueryDispatcher dispatcher;
        private readonly IPickupService pickupService;
        private readonly ILeadAssignmentLimitService leadAssignmentLimitService;
        private readonly ISecretaryAccessService secretaryAccessService;
        private readonly IConsultantProfileRepository consultantProfileRepository;

        public LeadAssignmentController(
            IQueryDispatcher dispatcher,
            IPickupService pickupService,
            ILeadAssignmentLimitService leadAssignmentLimitService,
            ISecretaryAccessService secretaryAccessService,
            IConsultantProfileRepository consultantProfileRepository)
        {
            this.dispatcher = dispatcher;
            this.pickupService = pickupService;
            this.leadAssignmentLimitService = leadAssignmentLimitService;
            this.secretaryAccessService = secretaryAccessService;
            this.consultantProfileRepository = consultantProfileRepository;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Get(
            [FromQuery] GetAllLeadsQuery query,
            CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
            var access = await secretaryAccessService.GetAccessAsync(userId, cancellationToken);
            if (access.IsSecretary &&
                (!await secretaryAccessService.HasPermissionAsync(userId,
                     DentalDashboard.Domain.Enums.SecretaryPermissionType.CreateReservation, cancellationToken) ||
                 !await secretaryAccessService.HasPermissionAsync(userId,
                     DentalDashboard.Domain.Enums.SecretaryPermissionType.ViewPatients, cancellationToken)))
                return StatusCode(StatusCodes.Status403Forbidden,
                    Result.Failure("شما دسترسی مشاهده بیماران و ایجاد رزرو را ندارید"));

            if (access.IsSecretary)
                query.ReservationOptionsOnly = true;

            var result = await dispatcher.DispatchAsync(query, cancellationToken);
            return Ok(result);
        }

        private bool TryGetCurrentUserId(out Guid userId)
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                        User.FindFirstValue("userId") ?? User.FindFirstValue("Id");
            return Guid.TryParse(value, out userId);
        }

        [HttpPost("{leadAssignmentId}/pickup")]
        [Authorize(Roles = "Consultant")]
        public async Task<IActionResult> Pickup(
            long leadAssignmentId,
            long consultantProfileId,
            CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUserId(out var userId)) return Unauthorized();

            var ownConsultantProfileId = await consultantProfileRepository.GetAll()
                .Where(profile => profile.UserId == userId && !profile.IsDeleted)
                .Select(profile => (long?)profile.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (!ownConsultantProfileId.HasValue) return Forbid();
            if (consultantProfileId != ownConsultantProfileId.Value) return Forbid();

            var result = await pickupService.PickupLeadAsync(
                leadAssignmentId,
                ownConsultantProfileId.Value,
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
