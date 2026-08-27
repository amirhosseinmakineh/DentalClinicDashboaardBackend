using DentalDashboard.Domain.Enums;

namespace DentalDashboard.Domain.Models;

public class Reservation : BaseAuditableEntity<long>
{
    public long LeadAssignmentId { get; set; }
    public LeadAssignment LeadAssignment { get; set; } = default!;
    public long ConsultantProfileId { get; set; }
    public ConsultantProfile ConsultantProfile { get; set; } = default!;
    public ReservationOwnerType? OwnerType { get; set; }
    public Guid? OwnerUserId { get; set; }
    public Guid? PatientUserId { get; set; }
    public User? PatientUser { get; set; }
    public DateTime ReservationAt { get; set; }
    public int PatientCount { get; set; } = 1;
    public string? DoctorName { get; set; }
    public ReservationType ReservationType { get; set; } = ReservationType.Regular;
    public List<DentalServiceType> DentalServices { get; set; } = [];
    public bool? PatientReceivedService { get; set; }
    public ReservationAttendanceConfirmationStatus AttendanceConfirmationStatus { get; set; } = ReservationAttendanceConfirmationStatus.PendingConsultantConfirmation;
    public DateTime? ConsultantAttendanceConfirmedAt { get; set; }
    public bool? ConsultantSaysPatientAttended { get; set; }
    public string? ConsultantAttendanceNote { get; set; }
    public DateTime? SecretaryReviewedAt { get; set; }
    public Guid? SecretaryUserId { get; set; }
    public bool? SecretaryApprovedConsultantConfirmation { get; set; }
    public string? SecretaryReviewNote { get; set; }
    public SecretaryAnnouncementStatus? SecretaryAnnouncementStatus { get; set; }
    public string? SecretaryAnnouncement { get; set; }
    // A secretary follow-up is stored on its existing reservation; no separate table is required.
    public bool? SecretaryFollowUpContacted { get; set; }
    public DateTime? SecretaryAnnouncementUpdatedAt { get; set; }
    public DateTime? SecretaryFollowUpCreatedAt { get; set; }
    public DateTime? SecretaryFollowUpDeletedAt { get; set; }
    public Guid? SecretaryAnnouncementUserId { get; set; }
    public bool IsAttendanceScoreApplied { get; set; }
    public int? AttendanceScoreValue { get; set; }
    public DateTime? AttendanceScoreAppliedAt { get; set; }
    public string? Description { get; set; }
    public string? AttendancePrediction { get; set; }
    public bool IsCanceled { get; set; }
    public DateTime? CanceledAt { get; set; }
    public DateTime InitialReservationAt { get; set; }
    public DateTime LastActivityAt { get; set; }
}
