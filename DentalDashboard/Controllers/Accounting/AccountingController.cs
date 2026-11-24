using System.Security.Claims;
using DentalDashboard.ApplicationService.Contract.Accounting;
using DentalDashboard.Domain.Accounting;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalDashboard.Controllers.Accounting;

[ApiController]
[RequestSizeLimit(65536)]
[Authorize(Roles = "Admin,Accountant")]
[ServiceFilter(typeof(AccountingExceptionFilter))]
[Route("api/accounting")]
public sealed class AccountingController(IQueryDispatcher queries, ICommandDispatcher commands, IAccountingAccess access) : ControllerBase
{
    private AccountingActor Actor => new(Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ??
        User.FindFirstValue("userId") ?? User.FindFirstValue("Id"), out var id) ? id : Guid.Empty);

    [HttpGet("access")]
    public async Task<IActionResult> Access(CancellationToken ct)
    {
        var result = await access.GetAsync(Actor.UserId, ct);
        return result is null ? Forbid() : Ok(new { result.IsAdmin, result.Permissions, availablePermissions = AccountingPermissions.All });
    }


    [HttpGet("overview")]
    public async Task<IActionResult> Overview([FromQuery] AccountingFilter filter, CancellationToken ct) =>
        Ok(await queries.DispatchAsync(new AccountingOverviewQuery(Actor, filter), ct));



    [HttpGet("sections/{section}")]
    public async Task<IActionResult> Section(AccountingSection section, [FromQuery] AccountingFilter filter, CancellationToken ct) =>
        Ok(await queries.DispatchAsync(new AccountingPageQuery(Actor, section, filter), ct));



    [HttpGet("cases/{caseId:guid}")]
    public async Task<IActionResult> Case(Guid caseId, CancellationToken ct)
    {
        var result = await queries.DispatchAsync(new AccountingCaseDetailsQuery(Actor, caseId), ct);
        return result is null ? NotFound() : Ok(result);
    }


    [HttpPost("operations/{operation}")]
    public async Task<IActionResult> Operate(string operation, AccountingOperationRequest request, CancellationToken ct) =>
        Ok(await commands.DispatchAsync(new AccountingOperationCommand(Actor, operation, request), ct));



    [HttpGet("export/{section}")]
    public async Task<IActionResult> Export(AccountingSection section, [FromQuery] AccountingFilter filter, CancellationToken ct, [FromQuery] bool excel = false)
    {
        var file = await queries.DispatchAsync(new AccountingExportQuery(Actor, section, filter, excel), ct);
        return File(file.Content, file.ContentType, file.FileName);
    }


    [HttpGet("transactions/{id:long}")]
    public async Task<IActionResult> Transaction(long id, CancellationToken ct)
    {
        var result = await queries.DispatchAsync(new AccountingTransactionQuery(Actor, id), ct);
        return result is null ? NotFound() : Ok(result);
    }


    [HttpGet("cases/{caseId:guid}/documents")]
    public async Task<IActionResult> Documents(Guid caseId, CancellationToken ct) =>
        Ok(await queries.DispatchAsync(new AccountingDocumentsQuery(Actor, caseId), ct));


    [HttpGet("cases/{caseId:guid}/documents/{documentId:guid}")]
    public async Task<IActionResult> Image(Guid caseId, Guid documentId, CancellationToken ct)
    {
        var file = await queries.DispatchAsync(new AccountingImageQuery(Actor, caseId, documentId), ct);
        return file is null ? NotFound() : File(file.Content, file.ContentType);
    }


    [HttpGet("transactions/{id:long}/receipt")]
    public async Task<IActionResult> Receipt(long id, CancellationToken ct)
    {
        var file = await queries.DispatchAsync(new AccountingReceiptQuery(Actor, id), ct);
        return file is null ? NotFound() : File(file.Content, file.ContentType, file.FileName);
    }



    [HttpGet("accountants")]
    public async Task<IActionResult> Accountants(CancellationToken ct) =>
        Ok(await queries.DispatchAsync(new AccountingUsersQuery(Actor), ct));



    [HttpPut("accountants/{userId:guid}/permissions")]
    public async Task<IActionResult> Permissions(Guid userId, SetAccountantPermissionsRequest request, CancellationToken ct) =>
        Ok(await commands.DispatchAsync(new SetAccountantPermissionsCommand(Actor, userId, request.Permissions), ct));
}
