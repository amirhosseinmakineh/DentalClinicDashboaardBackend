namespace DentalDashboard.Accounting.Contracts.Commands;

public sealed record CreateExpenseResponse(long Id, string Title, bool IsActive);
