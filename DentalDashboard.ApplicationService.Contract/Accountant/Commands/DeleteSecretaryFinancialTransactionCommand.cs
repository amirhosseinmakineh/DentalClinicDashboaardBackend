using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;

namespace DentalDashboard.ApplicationService.Contract.Accountant.Commands;

public sealed record DeleteSecretaryFinancialTransactionCommand(long Id) : ICommand;
