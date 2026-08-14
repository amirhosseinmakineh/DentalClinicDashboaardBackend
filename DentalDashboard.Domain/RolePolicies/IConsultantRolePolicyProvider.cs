using DentalDashboard.Domain.Enums;

namespace DentalDashboard.Domain.RolePolicies;

public interface IConsultantRolePolicyProvider
{
    ConsultantRolePolicy Get(ConsultantRole role);
    int GetDailyLimit(ConsultantRole role, LeadLimitType leadType, DateTime periodStartedAt, DateTime now);
    ConsultantRoleEvaluationDecision Evaluate(ConsultantRole role, int successfulPatientCount);
}
