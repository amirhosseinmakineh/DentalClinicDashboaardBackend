namespace DentalDashboard.Accountant.Application.Contracts.Commands;

public sealed record ExpenseCategoryResponse(
    long Id,
    string Title,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
