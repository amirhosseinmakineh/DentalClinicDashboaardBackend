using DentalDashboard.Domain.Enums;

namespace DentalDashboard.ApplicationService.Contract.Responses.ConsultantResponse;

public sealed record ActiveSellerConsultantResponse(
    long ConsultantId,
    ConsultantLevel CurrentRole,
    DateTime SellerStartedAt,
    int CurrentSellerDay,
    int AssignedNewLeadsToday,
    int AssignedBurnedLeadsToday);
