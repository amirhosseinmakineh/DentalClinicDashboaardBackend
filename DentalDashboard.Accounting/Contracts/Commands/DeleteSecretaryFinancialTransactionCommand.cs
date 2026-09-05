using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;

namespace DentalDashboard.Accounting.Contracts.Commands;

public sealed record DeleteSecretaryFinancialTransactionCommand(long Id) : ICommand;
