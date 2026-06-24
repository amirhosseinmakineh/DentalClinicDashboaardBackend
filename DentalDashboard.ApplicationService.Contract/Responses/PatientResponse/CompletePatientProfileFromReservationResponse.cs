namespace DentalDashboard.ApplicationService.Contract.Responses.PatientResponse
{
    public class CompletePatientProfileFromReservationResponse
    {
        public Guid UserId { get; set; }
        public long PatientProfileId { get; set; }
        public long ReservationId { get; set; }
        public long LeadAssignmentId { get; set; }
        public string PhoneNumber { get; set; } = default!;
        public string RoleName { get; set; } = default!;
    }
}
