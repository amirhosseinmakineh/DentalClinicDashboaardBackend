using DentalDashboard.Domain.Enums;

namespace DentalDashboard.Domain.RolePolicies;

public sealed record ConsultantRoleEvaluationDecision(
    ConsultantRole ResultingRole,
    ConsultantEvaluationResult Result,
    int RewardLevel,
    bool Deactivate);
