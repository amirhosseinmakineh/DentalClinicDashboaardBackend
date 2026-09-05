namespace DentalDashboard.Accounting.Contracts.Commands;

public sealed record ExpenseCategoryResponse(
    long Id,
    string Title,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
