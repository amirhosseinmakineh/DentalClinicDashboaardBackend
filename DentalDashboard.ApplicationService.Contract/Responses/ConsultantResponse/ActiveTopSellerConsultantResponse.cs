using DentalDashboard.Domain.Enums;

namespace DentalDashboard.ApplicationService.Contract.Responses.ConsultantResponse;

public sealed record ActiveTopSellerConsultantResponse(
    long ConsultantId,
    DateTime TopSellerStartedAt,
    DateTime CurrentWeekStart,
    int AssignedRealTimeToday,
    int SuccessfulPatientsThisWeek,
    bool IsActive,
    bool IsAvailable,
    bool IsOnline,
    TopSellerRewardLevel RewardLevel);
