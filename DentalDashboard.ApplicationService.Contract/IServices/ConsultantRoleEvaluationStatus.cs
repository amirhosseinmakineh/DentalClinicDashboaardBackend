using DentalDashboard.Domain.Enums;

namespace DentalDashboard.ApplicationService.Contract.IServices;

public record ConsultantRoleEvaluationStatus
{
    public ConsultantRole CurrentRole { get; init; }
    public DateTime PeriodStartedAt { get; init; }
    public DateTime NextEvaluationAt { get; init; }
    public int SuccessfulPatientCount { get; init; }
    public ConsultantEvaluationResult? LastEvaluationResult { get; init; }
    public DateTime? LastEvaluatedAt { get; init; }
}
