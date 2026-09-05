using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;

namespace DentalDashboard.Accounting.Contracts.Commands;

public sealed record DeleteExpenseCategoryCommand(long Id) : ICommand;
