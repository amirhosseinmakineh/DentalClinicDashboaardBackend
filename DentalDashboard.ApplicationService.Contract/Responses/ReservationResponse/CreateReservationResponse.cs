namespace DentalDashboard.ApplicationService.Contract.Responses.ReservationResponse
{
    public class CreateReservationResponse
    {
        public long Id { get; set; }
        public long LeadAssignmentId { get; set; }
        public long ConsultantProfileId { get; set; }
        public Guid? PatientUserId { get; set; }
        public bool RequiresPatientProfile { get; set; }
        public DateTime ReservationAt { get; set; }
        public string? SecondaryPhoneNumber { get; set; }
        public string PatientCity { get; set; } = default!;
        public string? PatientRegion { get; set; }
        public string? BusinessName { get; set; }
        public int? AttendanceProbabilityPercent { get; set; }
        public ReservationAttendanceConfirmationStatus AttendanceConfirmationStatus { get; set; }
        public string PatientName { get; set; } = default!;
        public string PatientPhoneNumber { get; set; } = default!;
    }
}
