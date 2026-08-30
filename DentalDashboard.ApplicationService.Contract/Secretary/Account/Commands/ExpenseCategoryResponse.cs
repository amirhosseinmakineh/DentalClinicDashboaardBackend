namespace DentalDashboard.ApplicationService.Contract.Secretary.Account.Commands;

public sealed record ExpenseCategoryResponse(
    long Id,
    string Title,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
