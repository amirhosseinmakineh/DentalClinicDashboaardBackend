using DentalDashboard.Domain.Models;

public class ConsultantProfile : BaseAuditableEntity<long>
{
    public ConsultantProfile()
    {
        CallAssignments = new HashSet<LeadAssignment>();
        Attendances = new HashSet<Attendance>();
        ScoreLogs = new HashSet<ScoreLog>();
    }
    public Guid UserId { get; set; }
    public string NationalCode { get; set; } = default!;
    public string Address { get; set; } = default!;
    public bool IsAvailable { get; set; } = false;
    public TimeSpan WorkStartTime { get; set; }
    public TimeSpan WorkEndTime { get; set; }
    public string? Notes { get; set; }
    public bool IsCompleteProfile { get; set; }
    public bool IsOnline { get; set; }
    public DateTime? LastOnlineAt { get; set; }
    public DateTime? LastOfflineAt { get; set; }
    public int CurrentScore { get; set; } = 0;
    #region Relations
    public User User { get; set; } = default!;
    public ICollection<LeadAssignment> CallAssignments { get; set; }
    public ICollection<Attendance> Attendances { get; set; }
    public ICollection<ScoreLog> ScoreLogs { get; set; }
    #endregion
}
