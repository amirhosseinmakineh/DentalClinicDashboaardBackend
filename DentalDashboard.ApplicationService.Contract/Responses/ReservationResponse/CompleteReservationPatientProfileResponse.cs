namespace DentalDashboard.ApplicationService.Contract.Responses.ReservationResponse
{
    public class CompleteReservationPatientProfileResponse
    {
        public long ReservationId { get; set; }
        public Guid PatientUserId { get; set; }
        public long PatientProfileId { get; set; }
        public long LeadAssignmentId { get; set; }
        public long ConsultantProfileId { get; set; }
        public DateTime ReservationAt { get; set; }
        public string PatientName { get; set; } = default!;
        public string PatientPhoneNumber { get; set; } = default!;
        public bool IsCompleteProfile { get; set; }
        public string RoleName { get; set; } = default!;
    }
}
