using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;

namespace DentalDashboard.Accountant.Application.Contracts.Commands;

public sealed record DeleteFinancialTransactionCommand(long Id) : ICommand;
