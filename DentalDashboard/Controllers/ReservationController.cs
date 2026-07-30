using DentalDashboard.ApplicationService.Contract.Requests.Reservation.Commands;
using DentalDashboard.ApplicationService.Contract.Requests.Reservation.Queries;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using DentalDashboard.Framwork.Domain;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Services;

namespace DentalDashboard.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReservationController : ControllerBase
    {
        private readonly ICommandDispatcher commandDispatcher;
        private readonly IQueryDispatcher queryDispatcher;
        private readonly SecretaryDashboardService secretaryDashboardService;

        public ReservationController(ICommandDispatcher commandDispatcher, IQueryDispatcher queryDispatcher, SecretaryDashboardService secretaryDashboardService)
        {
            this.commandDispatcher = commandDispatcher;
            this.queryDispatcher = queryDispatcher;
            this.secretaryDashboardService = secretaryDashboardService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateReservation(CreateReservationCommand command)
        {
            var result = await commandDispatcher.DispatchAsync(command);
            return Ok(result);
        }

        [HttpPost("CompletePatientProfile")]
        [Authorize(Roles = "Consultant")]
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
        [Authorize(Roles = "Secretary,Admin")]
        public async Task<IActionResult> GetSecretaryReservations([FromQuery] GetSecretaryReservationsQuery query)
        {
            var result = await queryDispatcher.DispatchAsync(query);
            return Ok(result);
        }

        [HttpGet("SecretaryDashboard")]
        [Authorize(Roles = "Secretary,Admin")]
        public async Task<IActionResult> GetSecretaryDashboard([FromQuery] DateOnly? date, [FromQuery] string? timeZone = "Asia/Tehran", [FromQuery] int listSize = 5, CancellationToken cancellationToken = default)
        {
            try { return Ok(Result<SecretaryDashboardDto>.Success(await secretaryDashboardService.GetAsync(date, timeZone, listSize, cancellationToken), "داشبورد منشی دریافت شد")); }
            catch (TimeZoneNotFoundException) { return BadRequest(Result.Failure("منطقه زمانی معتبر نیست")); }
        }

        [HttpPost("{reservationId:long}/secretary-confirm")]
        [Authorize(Roles = "Secretary")]
        public Task<IActionResult> SecretaryConfirm(long reservationId, SecretaryConfirmRequest request, CancellationToken ct) =>
            ReviewRequest(reservationId, ReservationRequestStatus.Confirmed, null, request.Note, null, null, ct);

        [HttpPost("{reservationId:long}/secretary-reschedule")]
        [Authorize(Roles = "Secretary")]
        public Task<IActionResult> SecretaryReschedule(long reservationId, SecretaryRescheduleRequest request, CancellationToken ct) =>
            ReviewRequest(reservationId, ReservationRequestStatus.Rescheduled, request.ReservationAt, request.Note, null, null, ct);

        [HttpPost("{reservationId:long}/secretary-reject")]
        [Authorize(Roles = "Secretary")]
        public Task<IActionResult> SecretaryReject(long reservationId, SecretaryRejectRequest request, CancellationToken ct) =>
            ReviewRequest(reservationId, ReservationRequestStatus.Rejected, null, null, request.ReasonCode, request.Reason, ct);

        [HttpPost("{reservationId:long}/patient-confirmation")]
        [Authorize(Roles = "Secretary")]
        public async Task<IActionResult> PatientConfirmation(long reservationId, PatientConfirmationRequest request, CancellationToken ct) =>
            await ExecuteMutation(async actor => await secretaryDashboardService.SetPatientConfirmationAsync(reservationId, actor, request.Confirmed, request.Note, ct));

        [HttpPost("{reservationId:long}/visit-result")]
        [Authorize(Roles = "Secretary")]
        public async Task<IActionResult> VisitResult(long reservationId, VisitResultRequest request, CancellationToken ct) =>
            await ExecuteMutation(async actor => await secretaryDashboardService.SetVisitResultAsync(reservationId, actor, request.VisitResultStatus, request.Note, ct));

        [HttpPost("{reservationId:long}/follow-ups")]
        [Authorize(Roles = "Secretary")]
        public async Task<IActionResult> CreateFollowUp(long reservationId, FollowUpRequest request, CancellationToken ct)
        {
            if (!TryGetAuthenticatedUserId(out var actor)) return Unauthorized(Result.Failure("شناسه کاربر در توکن معتبر نیست"));
            try { var id = await secretaryDashboardService.CreateFollowUpAsync(reservationId, actor, request, ct); return Ok(Result<object>.Success(new { followUpId = id }, "پیگیری ثبت شد")); }
            catch (Exception ex) when (ex is ArgumentException or KeyNotFoundException or InvalidOperationException) { return MutationError(ex); }
        }

        [HttpPut("{reservationId:long}/follow-ups/{followUpId:long}")]
        [Authorize(Roles = "Secretary")]
        public async Task<IActionResult> UpdateFollowUp(long reservationId, long followUpId, FollowUpRequest request, CancellationToken ct) =>
            await ExecuteMutation(async actor => await secretaryDashboardService.UpdateFollowUpAsync(reservationId, followUpId, actor, request, ct));

        private async Task<IActionResult> ReviewRequest(long id, ReservationRequestStatus status, DateTime? at, string? note, int? reasonCode, string? reason, CancellationToken ct)
        {
            if (!TryGetAuthenticatedUserId(out var actor)) return Unauthorized(Result.Failure("شناسه کاربر در توکن معتبر نیست"));
            try { return Ok(Result<ReservationMutationDto>.Success(await secretaryDashboardService.ReviewAsync(id, actor, status, at, note, reasonCode, reason, ct), "وضعیت درخواست رزرو ثبت شد")); }
            catch (Exception ex) when (ex is ArgumentException or KeyNotFoundException or InvalidOperationException) { return MutationError(ex); }
        }

        private async Task<IActionResult> ExecuteMutation(Func<Guid, Task> action)
        {
            if (!TryGetAuthenticatedUserId(out var actor)) return Unauthorized(Result.Failure("شناسه کاربر در توکن معتبر نیست"));
            try { await action(actor); return Ok(Result.Success("عملیات با موفقیت انجام شد")); }
            catch (Exception ex) when (ex is ArgumentException or KeyNotFoundException or InvalidOperationException) { return MutationError(ex); }
        }

        private IActionResult MutationError(Exception ex) => ex switch
        {
            KeyNotFoundException => NotFound(Result.Failure(ex.Message)),
            InvalidOperationException => Conflict(Result.Failure(ex.Message)),
            _ => BadRequest(Result.Failure(ex.Message))
        };

        [HttpPost("ConfirmAttendance")]
        public async Task<IActionResult> ConfirmAttendance(ConfirmReservationAttendanceCommand command)
        {
            var result = await commandDispatcher.DispatchAsync(command);
            return Ok(result);
        }

        [HttpPost("ReviewAttendance")]
        public async Task<IActionResult> ReviewAttendance(ReviewReservationAttendanceCommand command)
        {
            var result = await commandDispatcher.DispatchAsync(command);
            return Ok(result);
        }

        [HttpPut]
        [Authorize(Roles = "Consultant")]
        public async Task<IActionResult> UpdateReservation(UpdateReservationCommand command)
        {
            var result = await commandDispatcher.DispatchAsync(command);
            return Ok(result);
        }

        [HttpPost("SecretaryChangeTime")]
        [Authorize(Roles = "Secretary")]
        public async Task<IActionResult> SecretaryChangeTime(SecretaryChangeReservationTimeCommand command)
        {
            if (!TryGetAuthenticatedUserId(out var userId))
                return Ok(Result.Failure("شناسه کاربر در توکن معتبر نیست"));
            command.AuthenticatedUserId = userId;
            return Ok(await commandDispatcher.DispatchAsync(command));
        }

        [HttpPost("ConfirmSecretaryTimeChange")]
        [Authorize(Roles = "Consultant")]
        public async Task<IActionResult> ConfirmSecretaryTimeChange(ConfirmSecretaryTimeChangeCommand command)
        {
            if (!TryGetAuthenticatedUserId(out var userId))
                return Ok(Result.Failure("شناسه کاربر در توکن معتبر نیست"));
            command.AuthenticatedUserId = userId;
            return Ok(await commandDispatcher.DispatchAsync(command));
        }

        private bool TryGetAuthenticatedUserId(out Guid userId)
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("userId") ?? User.FindFirstValue("Id");
            return Guid.TryParse(value, out userId);
        }

        [HttpGet("ConsultantPatientProfiles")]
        public async Task<IActionResult> GetConsultantPatientProfiles(
            [FromQuery] GetConsultantPatientProfilesQuery query)
        {
            var result = await queryDispatcher.DispatchAsync(query);
            return Ok(result);
        }
    }

    public record SecretaryConfirmRequest(string? Note);
    public record SecretaryRescheduleRequest(DateTime ReservationAt, string? Note);
    public record SecretaryRejectRequest(int ReasonCode, string Reason);
    public record PatientConfirmationRequest(bool Confirmed, string? Note);
    public record VisitResultRequest(VisitResultStatus VisitResultStatus, string? Note);
}
