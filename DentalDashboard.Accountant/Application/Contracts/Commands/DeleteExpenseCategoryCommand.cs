using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;

namespace DentalDashboard.Accountant.Application.Contracts.Commands;

public sealed record DeleteExpenseCategoryCommand(long Id) : ICommand;
