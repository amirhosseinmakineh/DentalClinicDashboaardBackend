namespace DentalDashboard.ApplicationService.Contract.Responses.ReservationResponse
{
    public class CreateReservationResponse
    {
        public long Id { get; set; }
        public long LeadAssignmentId { get; set; }
        public long ConsultantProfileId { get; set; }
        public DateTime ReservationAt { get; set; }
        public string PatientName { get; set; } = default!;
        public string PatientPhoneNumber { get; set; } = default!;
        public Guid? PatientUserId { get; set; }
        public bool ShouldOpenPatientProfileDialog { get; set; }
    }
}
