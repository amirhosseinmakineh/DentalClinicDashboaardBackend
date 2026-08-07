namespace DentalDashboard.Domain.Strategies;

public sealed record SellerConsultantDecision(
    int CurrentSellerDay,
    bool CanReceiveNewLead,
    bool CanReceiveBurnedLead,
    int RemainingNewLeadCapacity,
    int RemainingBurnedLeadCapacity,
    bool IsReadyForEvaluation,
    bool ShouldPromoteToGold,
    bool ShouldRemainSeller,
    bool ShouldReturnToTest);
