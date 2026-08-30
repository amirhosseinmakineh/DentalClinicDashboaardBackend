using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;

namespace DentalDashboard.Accountant.Application.Contracts.Commands;

public sealed record CreateExpenseCommand : ICommand<CreateExpenseResponse>
{
    public string Title { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
