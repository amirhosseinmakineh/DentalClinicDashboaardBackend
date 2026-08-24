namespace DentalDashboard.ApplicationService.Contract.Responses.ReservationResponse;

public sealed class SecretaryReservationDetailsResponse
{
    public long Id { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string PatientPhoneNumber { get; set; } = string.Empty;
    public string ConsultantFullName { get; set; } = string.Empty;
    public DateTime ReservationAt { get; set; }
    public int PatientCount { get; set; }
    public string? DoctorName { get; set; }
}

public sealed class UpdateReservationDoctorResponse
{
    public long ReservationId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
}
