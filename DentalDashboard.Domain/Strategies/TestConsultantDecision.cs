namespace DentalDashboard.Domain.Strategies;

public sealed record TestConsultantDecision(
    int CurrentTestDay,
    bool CanReceiveNewLead,
    int RemainingDailyCapacity,
    bool IsFollowUpPhase,
    bool IsReadyForEvaluation,
    bool HasPassed);
