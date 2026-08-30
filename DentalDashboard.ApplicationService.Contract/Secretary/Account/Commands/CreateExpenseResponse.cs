namespace DentalDashboard.ApplicationService.Contract.Secretary.Account.Commands;

public sealed record CreateExpenseResponse(long Id, string Title, bool IsActive);
