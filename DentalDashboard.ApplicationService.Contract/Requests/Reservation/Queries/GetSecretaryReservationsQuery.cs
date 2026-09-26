using DentalDashboard.ApplicationService.Contract.Responses;
using DentalDashboard.ApplicationService.Contract.Responses.ReservationResponse;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using System.Text.Json.Serialization;

public sealed class GetSecretaryReservationsQuery
    : IQuery<PaginatedResult<SecretaryReservationItemResponse>>
{
    public long? ConsultantProfileId { get; set; }
    public string? Search { get; set; }
    public string? ConsultantName { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public DateOnly? Date { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public string? SearchText { get; set; }
    public ReservationType? ReservationType { get; set; }
    public SecretaryAnnouncementStatus? SecretaryAnnouncementStatus { get; set; }
    public ReservationAttendanceConfirmationStatus? AttendanceStatus { get; set; }
    public ReservationAttendanceConfirmationStatus? AttendanceConfirmationStatus { get; set; }
    public bool OnlyWaitingForSecretaryReview { get; set; }
    public bool OnlyConsultantAttendanceConfirmed { get; set; }
    public bool OnlyDue { get; set; }
    public string? ReservationStatus { get; set; }
    public bool IncludeCanceled { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string SortDirection { get; set; } = "asc";
    [JsonIgnore]
    public Guid SecretaryUserId { get; set; }
    [JsonIgnore]
    public bool IsAdmin { get; set; }
}
