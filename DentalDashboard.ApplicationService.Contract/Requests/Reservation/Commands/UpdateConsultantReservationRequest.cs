using DentalDashboard.Domain.Enums;

namespace DentalDashboard.ApplicationService.Contract.Requests.Reservation.Commands;

/// <summary>
/// Editable reservation fields exposed to the authenticated consultant dashboard.
/// The reservation and consultant identifiers are resolved by the API route and access token.
/// </summary>
public class UpdateConsultantReservationRequest
{
    public DateTime ReservationAt { get; set; }
    public DateTime? AppointmentDateTime { get; set; }
    public string? Description { get; set; }
    public string? PatientCity { get; set; }
    public string? PatientRegion { get; set; }
    public int? AttendanceProbabilityPercent { get; set; }
    public string? AttendancePrediction { get; set; }
    public string? SecondaryPhoneNumber { get; set; }
    public List<DentalServiceType>? DentalServices { get; set; }
}
