using DentalDashboard.ApplicationService.Contract.Requests.Reservation.Commands;
using DentalDashboard.ApplicationService.Contract.Requests.Reservation.Queries;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.Domain.IRepositories;
using Microsoft.EntityFrameworkCore;
using DentalDashboard.ApplicationService.Contract.Responses.ReservationResponse;
using DentalDashboard.Framwork.Domain;
using DentalDashboard.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace DentalDashboard.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReservationController : ControllerBase
    {
        private readonly ICommandDispatcher commandDispatcher;
        private readonly IQueryDispatcher queryDispatcher;
        private readonly ISecretaryAccessService secretaryAccessService;
        private readonly IConsultantProfileRepository consultantProfileRepository;
        private readonly IReservationRepository reservationRepository;
        private readonly IHubContext<ReservationsHub> reservationsHub;

        public ReservationController(ICommandDispatcher commandDispatcher, IQueryDispatcher queryDispatcher,
            ISecretaryAccessService secretaryAccessService,
            IConsultantProfileRepository consultantProfileRepository,
            IReservationRepository reservationRepository,
            IHubContext<ReservationsHub> reservationsHub)
        {
            this.commandDispatcher = commandDispatcher;
            this.queryDispatcher = queryDispatcher;
            this.secretaryAccessService = secretaryAccessService;
            this.consultantProfileRepository = consultantProfileRepository;
            this.reservationRepository = reservationRepository;
            this.reservationsHub = reservationsHub;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateReservation(CreateReservationCommand command)
        {
            if (TryGetCurrentUserId(out var userId))
            {
                var access = await secretaryAccessService.GetAccessAsync(userId);
                if (access.IsSecretary && !await secretaryAccessService.HasPermissionAsync(userId,
                    DentalDashboard.Domain.Enums.SecretaryPermissionType.CreateReservation))
                    return StatusCode(StatusCodes.Status403Forbidden,
                        Result.Failure("شما دسترسی ایجاد رزرو را ندارید"));
            }
            var result = await commandDispatcher.DispatchAsync(command);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPatch("SecretaryReservations/{reservationId:long}/time")]
        [Authorize]
        public async Task<IActionResult> UpdateSecretaryReservationTime(
            long reservationId,
            UpdateSecretaryReservationTimeRequest request,
            CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
            if (!await secretaryAccessService.HasPermissionAsync(userId,
                    DentalDashboard.Domain.Enums.SecretaryPermissionType.EditReservations) ||
                !await secretaryAccessService.CanAccessReservationAsync(userId, reservationId))
                return Forbid();

            var reservation = await reservationRepository.GetByIdAsync(reservationId);
            if (reservation == null || reservation.IsDeleted || reservation.IsCanceled)
                return Ok(Result<ReservationItemResponse>.Failure("رزرو فعال یافت نشد"));

            var command = new UpdateReservationCommand
            {
                ReservationId = reservationId,
                ConsultantProfileId = reservation.ConsultantProfileId,
                ReservationAt = request.ReservationAt,
                AppointmentDateTime = request.AppointmentDateTime,
                Description = reservation.Description,
                AttendancePrediction = reservation.AttendancePrediction,
                DentalServices = request.DentalServices,
                IsSecretaryEdit = true
            };

            var result = await commandDispatcher.DispatchAsync(command, cancellationToken);
            await BroadcastReservationUpdatedAsync(result, userId, cancellationToken);
            return Ok(result);
        }

        [HttpPost("CompletePatientProfile")]
        public async Task<IActionResult> CompletePatientProfile(CompleteReservationPatientProfileCommand command)
        {
            var result = await commandDispatcher.DispatchAsync(command);
            return Ok(result);
        }

        private async Task BroadcastReservationUpdatedAsync(
            Result<ReservationItemResponse> result,
            Guid? updatedByUserId = null,
            CancellationToken cancellationToken = default)
        {
            if (!result.IsSuccess || result.Data == null) return;

            await reservationsHub.Clients.All.SendAsync("ReservationUpdated", new
            {
                reservationId = result.Data.ReservationId,
                consultantProfileId = result.Data.ConsultantProfileId,
                reservationAt = result.Data.ReservationAt,
                appointmentDateTime = result.Data.AppointmentDateTime,
                updatedByUserId,
                updatedAt = DateTime.UtcNow,
                reservation = result.Data
            }, cancellationToken);
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
        public async Task<IActionResult> UpdateSecretaryAnnouncement(UpdateSecretaryAnnouncementCommand command )
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

                    command.IsSecretaryEdit = true;
                }
            }
            var result = await commandDispatcher.DispatchAsync(command);
            await BroadcastReservationUpdatedAsync(result, TryGetCurrentUserId(out var updatedBy) ? updatedBy : null);
            return Ok(result);
        }

        [HttpPut("ConsultantReservations/{reservationId:long}")]
        [Authorize]
        public async Task<IActionResult> UpdateConsultantReservation(
            long reservationId,
            UpdateConsultantReservationRequest request,
            CancellationToken cancellationToken)
        {
            if (!TryGetCurrentUserId(out var userId)) return Unauthorized();

            var consultantProfileId = await consultantProfileRepository.GetAll()
                .Where(x => x.UserId == userId && !x.IsDeleted)
                .Select(x => (long?)x.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (!consultantProfileId.HasValue) return Forbid();

            var command = new UpdateReservationCommand
            {
                ReservationId = reservationId,
                ConsultantProfileId = consultantProfileId.Value,
                ReservationAt = request.ReservationAt,
                AppointmentDateTime = request.AppointmentDateTime,
                Description = request.Description,
                PatientCity = request.PatientCity,
                PatientRegion = request.PatientRegion,
                AttendanceProbabilityPercent = request.AttendanceProbabilityPercent,
                AttendancePrediction = request.AttendancePrediction,
                SecondaryPhoneNumber = request.SecondaryPhoneNumber,
                DentalServices = request.DentalServices
            };

            var result = await commandDispatcher.DispatchAsync(command, cancellationToken);
            await BroadcastReservationUpdatedAsync(result, userId, cancellationToken);
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
