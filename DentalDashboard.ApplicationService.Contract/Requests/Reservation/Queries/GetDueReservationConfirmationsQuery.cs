using DentalDashboard.ApplicationService.Contract.Responses.ReservationResponse;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;

namespace DentalDashboard.ApplicationService.Contract.Requests.Reservation.Queries
{
    public class GetDueReservationConfirmationsQuery : IQuery<List<ReservationItemResponse>>
    {
        public long ConsultantProfileId { get; set; }
        public DateTime? Now { get; set; }
    }
}
