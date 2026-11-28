using System.Security.Claims;
using DentalDashboard.ApplicationService.Contract.Accounting;
using DentalDashboard.ApplicationService.Contract.Accounting.Reminders;
using DentalDashboard.Domain.Accounting.Reminders;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalDashboard.Controllers.Accounting;

[ApiController]
[Authorize(Roles = "Admin,Accountant")]
[Route("api/accounting/reminders")]
[ServiceFilter(typeof(AccountingExceptionFilter))]
public sealed class AccountingRemindersController(IQueryDispatcher queries, ICommandDispatcher commands) : ControllerBase
{
    private AccountingActor Actor => new(Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ??
        User.FindFirstValue("userId") ?? User.FindFirstValue("Id"), out var id) ? id : Guid.Empty);

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] AccountingReminderKind kind, [FromQuery] long[] ids, CancellationToken ct) =>
        Ok(await queries.DispatchAsync(new AccountingRemindersQuery(Actor, kind, ids), ct));

    [HttpPut("{kind}/{targetId:long}")]
    public async Task<IActionResult> Set(AccountingReminderKind kind, long targetId, SetReminderRequest request, CancellationToken ct) =>
        Ok(await commands.DispatchAsync(new SetAccountingReminderCommand(Actor, kind, targetId, request.Enabled), ct));

    [HttpPost("test")]
    public async Task<IActionResult> Test(CancellationToken ct) =>
        Ok(await commands.DispatchAsync(new TestAccountingReminderCommand(Actor), ct));
}

public sealed record SetReminderRequest(bool Enabled);
