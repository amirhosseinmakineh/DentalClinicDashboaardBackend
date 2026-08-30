namespace DentalDashboard.Accountant.Application.Contracts.Commands;

public sealed record CreateExpenseResponse(long Id, string Title, bool IsActive);
