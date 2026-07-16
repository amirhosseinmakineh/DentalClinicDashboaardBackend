namespace DentalDashboard.ApplicationService.Contract.Responses.ReservationResponse
{
    public class ConsultantPatientProfileItemResponse
    {
        public long ReservationId { get; set; }
        public long LeadAssignmentId { get; set; }
        public Guid PatientUserId { get; set; }
        public long PatientProfileId { get; set; }
        public string PatientName { get; set; } = default!;
        public string PatientPhoneNumber { get; set; } = default!;
        public string? PatientCity { get; set; }
        public string? PatientRegion { get; set; }
        public DateTime ProfileCreatedAt { get; set; }
        public DateTime ReservationAt { get; set; }
        public string? InsuranceName { get; set; }
        public string? EmergencyPhoneNumber { get; set; }
    }
}
