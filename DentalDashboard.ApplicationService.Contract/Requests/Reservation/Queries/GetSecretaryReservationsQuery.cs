using DentalDashboard.ApplicationService.Contract.Responses;
using DentalDashboard.ApplicationService.Contract.Responses.ReservationResponse;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;

namespace DentalDashboard.ApplicationService.Contract.Requests.Reservation.Queries
{
    public class GetSecretaryReservationsQuery : IQuery<PaginatedResult<SecretaryReservationItemResponse>>
    {
        public long? ConsultantProfileId { get; set; }
        public DateOnly? Date { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public ReservationAttendanceConfirmationStatus? AttendanceConfirmationStatus { get; set; }
        public bool OnlyWaitingForSecretaryReview { get; set; } = false;
        public bool OnlyConsultantAttendanceConfirmed { get; set; } = false;
        public bool IncludeCanceled { get; set; } = false;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
