using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;

namespace DentalDashboard.ApplicationService.Contract.Secretary.Accountant.Commands;

public sealed record DeleteExpenseCategoryCommand(long Id) : ICommand;
