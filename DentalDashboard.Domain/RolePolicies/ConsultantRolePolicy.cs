using DentalDashboard.Domain.Enums;

namespace DentalDashboard.Domain.RolePolicies;

public sealed record ConsultantRolePolicy(
    ConsultantRole Role,
    TimeSpan EvaluationPeriod,
    int RealtimeDailyLimit,
    int BurntDailyLimit,
    TimeSpan? LeadReceptionPeriod,
    int PromotionThreshold,
    int DemotionThreshold,
    int RewardThreshold,
    int HigherRewardThreshold);
