using DentalDashboard.ApplicationService.Contract.Responses;
using DentalDashboard.ApplicationService.Contract.Responses.ReservationResponse;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;

namespace DentalDashboard.ApplicationService.Contract.Requests.Reservation.Queries
{
    public class GetConsultantReservationsQuery : IQuery<PaginatedResult<ReservationItemResponse>>
    {
        public long ConsultantProfileId { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public ReservationAttendanceConfirmationStatus? AttendanceConfirmationStatus { get; set; }
        public bool OnlyDueForConsultantConfirmation { get; set; } = false;
        public bool OnlySecretaryReviewed { get; set; } = false;
        public string? PatientName { get; set; }
        public string? PatientPhoneNumber { get; set; }
        public bool IncludeCanceled { get; set; } = false;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
