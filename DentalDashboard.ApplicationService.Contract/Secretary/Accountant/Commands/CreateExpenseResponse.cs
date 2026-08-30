namespace DentalDashboard.ApplicationService.Contract.Secretary.Accountant.Commands;

public sealed record CreateExpenseResponse(long Id, string Title, bool IsActive);
