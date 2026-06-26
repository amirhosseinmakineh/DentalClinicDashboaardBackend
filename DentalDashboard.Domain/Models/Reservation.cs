namespace DentalDashboard.Domain.Models;

public class Reservation : BaseAuditableEntity<long>
{
    public long LeadAssignmentId { get; set; }
    public LeadAssignment LeadAssignment { get; set; } = default!;
    public long ConsultantProfileId { get; set; }
    public ConsultantProfile ConsultantProfile { get; set; } = default!;
    public Guid? PatientUserId { get; set; }
    public User? PatientUser { get; set; }
    public DateTime ReservationAt { get; set; }
    public string PatientCity { get; set; } = default!;
    public int AttendanceProbabilityPercent { get; set; }
    public string AttendancePrediction { get; set; } = default!;
    public ReservationAttendanceConfirmationStatus AttendanceConfirmationStatus { get; set; } = ReservationAttendanceConfirmationStatus.PendingConsultantConfirmation;
    public DateTime? ConsultantAttendanceConfirmedAt { get; set; }
    public bool? ConsultantSaysPatientAttended { get; set; }
    public string? ConsultantAttendanceNote { get; set; }
    public DateTime? SecretaryReviewedAt { get; set; }
    public Guid? SecretaryUserId { get; set; }
    public bool? SecretaryApprovedConsultantConfirmation { get; set; }
    public string? SecretaryReviewNote { get; set; }
    public bool IsAttendanceScoreApplied { get; set; }
    public int? AttendanceScoreValue { get; set; }
    public DateTime? AttendanceScoreAppliedAt { get; set; }
    public string? Description { get; set; }
    public bool IsCanceled { get; set; }
    public DateTime? CanceledAt { get; set; }
}