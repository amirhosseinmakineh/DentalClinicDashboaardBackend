using DentalDashboard.Domain.Enums;

namespace DentalDashboard.ApplicationService.Contract.Responses.ReservationResponse
{
    public class SecretaryReservationItemResponse
    {
        public long Id { get; set; }
        public long LeadAssignmentId { get; set; }
        public long ConsultantProfileId { get; set; }
        public Guid ConsultantUserId { get; set; }
        public string ConsultantFullName { get; set; } = default!;
        public Guid? PatientUserId { get; set; }
        public bool RequiresPatientProfile { get; set; }
        public DateTime ReservationAt { get; set; }
        public string PatientName { get; set; } = default!;
        public string PatientPhoneNumber { get; set; } = default!;
        public string? SecondaryPhoneNumber { get; set; }
        public string PatientCity { get; set; } = default!;
        public string? PatientRegion { get; set; }
        public string? BusinessName { get; set; }
        public int? AttendanceProbabilityPercent { get; set; }
        public ReservationAttendanceConfirmationStatus AttendanceConfirmationStatus { get; set; }
        public DateTime? ConsultantAttendanceConfirmedAt { get; set; }
        public bool? ConsultantSaysPatientAttended { get; set; }
        public string? ConsultantAttendanceNote { get; set; }
        public bool IsWaitingForSecretaryReview { get; set; }
        public bool IsReservationDue { get; set; }
        public DateTime? SecretaryReviewedAt { get; set; }
        public Guid? SecretaryUserId { get; set; }
        public bool? SecretaryApprovedConsultantConfirmation { get; set; }
        public string? SecretaryReviewNote { get; set; }
        public bool IsAttendanceScoreApplied { get; set; }
        public int? AttendanceScoreValue { get; set; }
        public DateTime? AttendanceScoreAppliedAt { get; set; }
        public string? Description { get; set; }
        public bool IsCanceled { get; set; }
    }
}
