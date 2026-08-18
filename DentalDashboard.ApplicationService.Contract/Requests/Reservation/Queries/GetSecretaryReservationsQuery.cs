using DentalDashboard.ApplicationService.Contract.Responses;
using DentalDashboard.ApplicationService.Contract.Responses.ReservationResponse;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;

namespace DentalDashboard.ApplicationService.Contract.Requests.Reservation.Queries
{
    public class GetSecretaryReservationsQuery : IQuery<PaginatedResult<SecretaryReservationItemResponse>>
    {
        public long? ConsultantProfileId { get; set; }
        public string SearchText { get; set; } = string.Empty;
        public string Search { get; set; } = string.Empty;
        public string ConsultantName { get; set; } = string.Empty;
        public DateOnly? Date { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public long? ConsultantId { get; set; }
        public SecretaryAnnouncementStatus? SecretaryAnnouncementStatus { get; set; }
        public ReservationAttendanceConfirmationStatus? ReservationStatus { get; set; }
        public ReservationAttendanceConfirmationStatus? AttendanceConfirmationStatus { get; set; }
        public bool OnlyWaitingForSecretaryReview { get; set; } = false;
        public bool OnlyConsultantAttendanceConfirmed { get; set; } = false;
        public bool IncludeCanceled { get; set; } = false;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string SortDirection { get; set; } = string.Empty;
    }
}
