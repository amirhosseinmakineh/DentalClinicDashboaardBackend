using DentalDashboard.Domain.Enums;

namespace DentalDashboard.ApplicationService.Contract.Requests.Reservation.Commands;

/// <summary>
/// Changes only the appointment time from the authenticated secretary dashboard.
/// Reservation ownership is resolved by the API and cannot be supplied by the client.
/// </summary>
public class UpdateSecretaryReservationTimeRequest
{
    public DateTime ReservationAt { get; set; }
    public DateTime? AppointmentDateTime { get; set; }
    public int? PatientCount { get; set; }
    public List<DentalServiceType>? DentalServices { get; set; }
}
