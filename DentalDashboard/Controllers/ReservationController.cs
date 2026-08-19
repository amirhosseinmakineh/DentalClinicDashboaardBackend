using DentalDashboard.ApplicationService.Contract.Requests.Reservation.Commands;
using DentalDashboard.ApplicationService.Contract.Requests.Reservation.Queries;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using DentalDashboard.ApplicationService.Contract.IServices;

namespace DentalDashboard.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReservationController : ControllerBase
    {
        private readonly ICommandDispatcher commandDispatcher;
        private readonly IQueryDispatcher queryDispatcher;
        private readonly ISecretaryAccessService secretaryAccessService;

        public ReservationController(ICommandDispatcher commandDispatcher, IQueryDispatcher queryDispatcher,
            ISecretaryAccessService secretaryAccessService)
        {
            this.commandDispatcher = commandDispatcher;
            this.queryDispatcher = queryDispatcher;
            this.secretaryAccessService = secretaryAccessService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateReservation(CreateReservationCommand command)
        {
            if (TryGetCurrentUserId(out var userId))
            {
                var access = await secretaryAccessService.GetAccessAsync(userId);
                if (access.IsSecretary && !await secretaryAccessService.HasPermissionAsync(userId,
                    DentalDashboard.Domain.Enums.SecretaryPermissionType.CreateReservation)) return Forbid();
            }
            var result = await commandDispatcher.DispatchAsync(command);
            return Ok(result);
        }

        [HttpPost("CompletePatientProfile")]
        public async Task<IActionResult> CompletePatientProfile(CompleteReservationPatientProfileCommand command)
        {
            var result = await commandDispatcher.DispatchAsync(command);
            return Ok(result);
        }

        [HttpGet("GetConsultantReservations")]
        public async Task<IActionResult> GetConsultantReservations([FromQuery] GetConsultantReservationsQuery query)
        {
            var result = await queryDispatcher.DispatchAsync(query);
            return Ok(result);
        }

        [HttpGet("DueConfirmations")]
        public async Task<IActionResult> GetDueConfirmations([FromQuery] GetDueReservationConfirmationsQuery query)
        {
            var result = await queryDispatcher.DispatchAsync(query);
            return Ok(result);
        }

        [HttpGet("SecretaryReservations")]
        [Authorize]
        public async Task<IActionResult> GetSecretaryReservations([FromQuery] GetSecretaryReservationsQuery query)
        {
            if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
            if (!await secretaryAccessService.HasPermissionAsync(userId, DentalDashboard.Domain.Enums.SecretaryPermissionType.ViewReservations)) return Forbid();
            query.SecretaryUserId = userId;
            var result = await queryDispatcher.DispatchAsync(query);
            return Ok(result);
        }

        [HttpGet("/api/reservations")]
        [Authorize]
        public async Task<IActionResult> GetReservations([FromQuery] GetSecretaryReservationsQuery query)
        {
            if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
            if (!await secretaryAccessService.HasPermissionAsync(userId, DentalDashboard.Domain.Enums.SecretaryPermissionType.ViewReservations)) return Forbid();
            query.SecretaryUserId = userId;
            var result = await queryDispatcher.DispatchAsync(query);
            return Ok(result);
        }

        [HttpPost("ConfirmAttendance")]
        public async Task<IActionResult> ConfirmAttendance(ConfirmReservationAttendanceCommand command)
        {
            var result = await commandDispatcher.DispatchAsync(command);
            return Ok(result);
        }

        [HttpPost("ReviewAttendance")]
        [Authorize]
        public async Task<IActionResult> ReviewAttendance(ReviewReservationAttendanceCommand command)
        {
            if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
            command.SecretaryUserId = userId;
            if (!await secretaryAccessService.HasPermissionAsync(userId, DentalDashboard.Domain.Enums.SecretaryPermissionType.ConfirmAttendance) ||
                !await secretaryAccessService.CanAccessReservationAsync(userId, command.ReservationId)) return Forbid();
            var result = await commandDispatcher.DispatchAsync(command);
            return Ok(result);
        }

        [HttpPut("SecretaryAnnouncement")]
        [Authorize]
        public async Task<IActionResult> UpdateSecretaryAnnouncement(UpdateSecretaryAnnouncementCommand command)
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                              User.FindFirstValue("userId") ??
                              User.FindFirstValue("Id");
            if (!Guid.TryParse(userIdValue, out var secretaryUserId))
                return Unauthorized();

            command.SecretaryUserId = secretaryUserId;
            if (!await secretaryAccessService.HasPermissionAsync(secretaryUserId, DentalDashboard.Domain.Enums.SecretaryPermissionType.SecretaryAnnouncement) ||
                !await secretaryAccessService.CanAccessReservationAsync(secretaryUserId, command.ReservationId))
                return Forbid();
            var result = await commandDispatcher.DispatchAsync(command);
            return Ok(result);
        }

        [HttpPut]
        [Authorize]
        public async Task<IActionResult> UpdateReservation(UpdateReservationCommand command)
        {
            if (TryGetCurrentUserId(out var userId))
            {
                var access = await secretaryAccessService.GetAccessAsync(userId);
                if (access.IsSecretary)
                {
                    if (!await secretaryAccessService.HasPermissionAsync(userId,
                            DentalDashboard.Domain.Enums.SecretaryPermissionType.EditReservations) ||
                        !await secretaryAccessService.CanAccessReservationAsync(userId, command.ReservationId)) return Forbid();
                }
            }
            var result = await commandDispatcher.DispatchAsync(command);
            return Ok(result);
        }

        private bool TryGetCurrentUserId(out Guid userId)
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("userId") ?? User.FindFirstValue("Id");
            return Guid.TryParse(value, out userId);
        }

        [HttpGet("ConsultantPatientProfiles")]
        public async Task<IActionResult> GetConsultantPatientProfiles(
            [FromQuery] GetConsultantPatientProfilesQuery query)
        {
            if (TryGetCurrentUserId(out var userId))
            {
                var access = await secretaryAccessService.GetAccessAsync(userId);
                if (access.IsSecretary && !await secretaryAccessService.HasPermissionAsync(userId,
                    DentalDashboard.Domain.Enums.SecretaryPermissionType.ViewPatients)) return Forbid();
            }
            var result = await queryDispatcher.DispatchAsync(query);
            return Ok(result);
        }
    }
}
