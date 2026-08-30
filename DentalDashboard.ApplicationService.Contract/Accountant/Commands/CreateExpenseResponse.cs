namespace DentalDashboard.ApplicationService.Contract.Accountant.Commands;

public sealed record CreateExpenseResponse(long Id, string Title, bool IsActive);
