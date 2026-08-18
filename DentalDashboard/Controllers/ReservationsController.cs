using DentalDashboard.ApplicationService.Contract.Requests.Reservation.Queries;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using Microsoft.AspNetCore.Mvc;

namespace DentalDashboard.Controllers;

[Route("api/reservations")]
[ApiController]
public class ReservationsController : ControllerBase
{
    private readonly IQueryDispatcher queryDispatcher;

    public ReservationsController(IQueryDispatcher queryDispatcher)
    {
        this.queryDispatcher = queryDispatcher;
    }

    [HttpGet]
    public async Task<IActionResult> GetReservations(
        [FromQuery] string? search,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] long? consultantId,
        [FromQuery] SecretaryAnnouncementStatus? secretaryAnnouncementStatus,
        [FromQuery] ReservationStatus? reservationStatus,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var query = new GetSecretaryReservationsQuery
        {
            SearchText = search ?? string.Empty,
            From = fromDate,
            To = toDate,
            ConsultantProfileId = consultantId,
            SecretaryAnnouncementStatus = secretaryAnnouncementStatus,
            ReservationStatus = reservationStatus,
            IncludeCanceled = reservationStatus == null,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var result = await queryDispatcher.DispatchAsync(query, cancellationToken);
        return Ok(result);
    }
}
