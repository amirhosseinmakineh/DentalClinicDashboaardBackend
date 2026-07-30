namespace DentalDashboard.ApplicationService.Contract.Responses.ReservationResponse;

public class ReservationTimeChangeResponse
{
    public long ReservationId { get; set; }
    public DateTime ReservationAt { get; set; }
    public bool IsWaitingForConsultantTimeConfirmation { get; set; }
    public string? SecretaryTimeChangeNote { get; set; }
    public DateTime? SecretaryChangedReservationAt { get; set; }
}
