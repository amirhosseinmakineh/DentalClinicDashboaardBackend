using DentalDashboard.ApplicationService.Contract.Accounting;
using DentalDashboard.ApplicationService.Contract.Accounting.Reminders;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;

namespace DentalDashboard.ApplicationService.Accounting.Reminders;

public sealed class AccountingRemindersHandler(AccountingReminderService service) : IQueryHandler<AccountingRemindersQuery, IReadOnlyList<long>>
{
    public Task<IReadOnlyList<long>> HandleAsync(AccountingRemindersQuery query, CancellationToken ct = default) => service.GetAsync(query, ct);
}

public sealed class SetAccountingReminderHandler(AccountingReminderService service) : ICommandHandler<SetAccountingReminderCommand, AccountingOperationResponse>
{
    public async Task<Result<AccountingOperationResponse>> HandleAsync(SetAccountingReminderCommand command, CancellationToken ct = default)
    {
        var response = await service.SetAsync(command, ct);
        return Result<AccountingOperationResponse>.Success(response, response.Message);
    }
}

public sealed class TestAccountingReminderHandler(AccountingReminderService service) : ICommandHandler<TestAccountingReminderCommand, AccountingOperationResponse>
{
    public async Task<Result<AccountingOperationResponse>> HandleAsync(TestAccountingReminderCommand command, CancellationToken ct = default)
    {
        var response = await service.TestAsync(command, ct);
        return Result<AccountingOperationResponse>.Success(response, response.Message);
    }
}
