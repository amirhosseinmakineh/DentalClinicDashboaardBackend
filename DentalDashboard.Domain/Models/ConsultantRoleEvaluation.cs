using DentalDashboard.Domain.Enums;

namespace DentalDashboard.Domain.Models;

public class ConsultantRoleEvaluation : BaseAuditableEntity<long>
{
    public long ConsultantProfileId { get; set; }
    public ConsultantProfile ConsultantProfile { get; set; } = default!;
    public ConsultantRole EvaluatedRole { get; set; }
    public ConsultantRole? ResultingRole { get; set; }
    public DateTime PeriodStartedAt { get; set; }
    public DateTime PeriodEndedAt { get; set; }
    public DateTime EvaluatedAt { get; set; }
    public int SuccessfulPatientCount { get; set; }
    public ConsultantEvaluationResult Result { get; set; }
    public int RewardLevel { get; set; }
}
