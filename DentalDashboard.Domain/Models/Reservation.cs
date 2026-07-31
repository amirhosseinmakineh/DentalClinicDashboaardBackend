using DentalDashboard.Domain.Enums;

namespace DentalDashboard.Domain.Models;

public class Reservation : BaseAuditableEntity<long>
{
    public ICollection<ReservationFollowUp> FollowUps { get; set; } = new List<ReservationFollowUp>();
    public ICollection<SecretaryReservationActivity> SecretaryActivities { get; set; } = new List<SecretaryReservationActivity>();
    public ICollection<ReservationTimeChange> ReservationTimeChanges { get; set; } = new List<ReservationTimeChange>();
    public long LeadAssignmentId { get; set; }
    public LeadAssignment LeadAssignment { get; set; } = default!;
    public long ConsultantProfileId { get; set; }
    public ConsultantProfile ConsultantProfile { get; set; } = default!;
    public Guid? PatientUserId { get; set; }
    public User? PatientUser { get; set; }
    public DateTime ReservationAt { get; set; }
    public DateTime InitialReservationAt { get; set; }
    public ReservationRequestStatus ReservationRequestStatus { get; set; } = ReservationRequestStatus.PendingSecretaryReview;
    public VisitResultStatus VisitResultStatus { get; set; } = VisitResultStatus.Pending;
    public bool? IsConfirmedWithPatient { get; set; }
    public DateTime? ConfirmedWithPatientAt { get; set; }
    public Guid? ConfirmedWithPatientByUserId { get; set; }
    public string? PatientConfirmationNote { get; set; }
    public DateTime? RequestReviewedAt { get; set; }
    public Guid? RequestReviewedByUserId { get; set; }
    public string? RequestReviewNote { get; set; }
    public int? RejectionReasonCode { get; set; }
    public string? RejectionReason { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime? VisitResultRecordedAt { get; set; }
    public Guid? VisitResultRecordedByUserId { get; set; }
    public string? VisitResultNote { get; set; }
    public DateTime LastActivityAt { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
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
    public string? AttendancePrediction { get; set; }
    public bool IsCanceled { get; set; }
    public DateTime? CanceledAt { get; set; }
    public ICollection<ReservationContactLog> ContactLogs { get; set; } = new List<ReservationContactLog>();
    public ICollection<ReservationNote> Notes { get; set; } = new List<ReservationNote>();
}
