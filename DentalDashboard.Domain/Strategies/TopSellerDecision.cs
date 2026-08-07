using DentalDashboard.Domain.Enums;

namespace DentalDashboard.Domain.Strategies;

public sealed record TopSellerDecision(
    bool CanReceiveRealTimeLead,
    int RemainingRealTimeCapacity,
    bool CanReceiveBurnedLead,
    bool IsReadyForWeeklyEvaluation,
    bool ShouldRemainTopSeller,
    bool ShouldDowngradeToSeller,
    TopSellerRewardLevel RewardLevel);
