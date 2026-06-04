using DentalDashboard.Domain.Models;

public class ScoreLog : BaseAuditableEntity<long>
{
    public long ConsultantProfileId { get; set; }

    public ScoreType ScoreType { get; set; }

    public int ScoreValue { get; set; }

    public string? Description { get; set; }

    public ConsultantProfile ConsultantProfile { get; set; } = default!;
}
