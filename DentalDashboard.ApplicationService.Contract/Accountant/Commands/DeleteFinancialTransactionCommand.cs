using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;

namespace DentalDashboard.ApplicationService.Contract.Accountant.Commands;

public sealed record DeleteFinancialTransactionCommand(long Id) : ICommand;
