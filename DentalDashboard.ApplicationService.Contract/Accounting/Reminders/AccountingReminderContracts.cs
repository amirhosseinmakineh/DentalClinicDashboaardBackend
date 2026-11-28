using DentalDashboard.Domain.Accounting.Reminders;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;

namespace DentalDashboard.ApplicationService.Contract.Accounting.Reminders;

public sealed record AccountingRemindersQuery(AccountingActor Actor, AccountingReminderKind Kind, long[] Ids) : IQuery<IReadOnlyList<long>>;
public sealed record SetAccountingReminderCommand(AccountingActor Actor, AccountingReminderKind Kind, long TargetId, bool Enabled) : ICommand<AccountingOperationResponse>;
public sealed record TestAccountingReminderCommand(AccountingActor Actor) : ICommand<AccountingOperationResponse>;
