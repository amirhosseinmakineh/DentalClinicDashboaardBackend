using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;

namespace DentalDashboard.ApplicationService.Contract.Secretary.Account.Commands;

public sealed record DeleteSecretaryFinancialTransactionCommand(long Id) : ICommand;
