namespace DentalDashboard.Domain.Models;

public class Attendance : BaseAuditableEntity<long>
{
    public long ConsultantProfileId { get; set; }

    public DateOnly AttendanceDate { get; set; }

    public TimeOnly CheckInTime { get; set; }

    public TimeOnly CheckOutTime { get; set; }

    public AttendanceStatus Status { get; set; }

    public string? Description { get; set; }

    public ConsultantProfile ConsultantProfile { get; set; } = default!;
}
