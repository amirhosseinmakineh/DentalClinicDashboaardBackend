using DentalDashboard.ApplicationService.Contract.Requests.Reservation.Commands;
using DentalDashboard.ApplicationService.Contract.Requests.Reservation.Queries;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using Microsoft.AspNetCore.Mvc;

namespace DentalDashboard.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReservationController : ControllerBase
    {
        private readonly ICommandDispatcher commandDispatcher;
        private readonly IQueryDispatcher queryDispatcher;

        public ReservationController(ICommandDispatcher commandDispatcher, IQueryDispatcher queryDispatcher)
        {
            this.commandDispatcher = commandDispatcher;
            this.queryDispatcher = queryDispatcher;
        }

        [HttpPost]
        public async Task<IActionResult> CreateReservation(CreateReservationCommand command)
        {
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
        public async Task<IActionResult> GetSecretaryReservations([FromQuery] GetSecretaryReservationsQuery query)
        {
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
        public async Task<IActionResult> ReviewAttendance(ReviewReservationAttendanceCommand command)
        {
            var result = await commandDispatcher.DispatchAsync(command);
            return Ok(result);
        }
    }
}
