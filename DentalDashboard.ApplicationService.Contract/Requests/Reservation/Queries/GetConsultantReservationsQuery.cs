using DentalDashboard.ApplicationService.Contract.Responses;
using DentalDashboard.ApplicationService.Contract.Responses.ReservationResponse;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;

namespace DentalDashboard.ApplicationService.Contract.Requests.Reservation.Queries
{
    public class GetConsultantReservationsQuery : IQuery<PaginatedResult<ReservationItemResponse>>
    {
        public long ConsultantProfileId { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public bool IncludeCanceled { get; set; } = false;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
