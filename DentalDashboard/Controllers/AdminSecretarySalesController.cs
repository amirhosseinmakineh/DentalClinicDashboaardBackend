using System.Security.Claims;
using DentalDashboard.ApplicationService.Contract.Secretary.Accountant.SecretarySales.Commands;
using DentalDashboard.ApplicationService.Contract.Secretary.Accountant.SecretarySales.Queries;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalDashboard.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/secretary-sales")]
public sealed class AdminSecretarySalesController(ICommandDispatcher commands, IQueryDispatcher queries) : ControllerBase
{
    [HttpGet("services")]
    public async Task<IActionResult> Services([FromQuery] GetSecretarySaleServicesQuery query, CancellationToken cancellationToken) =>
        Ok(await queries.DispatchAsync(query, cancellationToken));

    [HttpPost("services")]
    public async Task<IActionResult> CreateService(CreateSecretarySaleServiceCommand command, CancellationToken cancellationToken) =>
        Write(await commands.DispatchAsync(command, cancellationToken));

    [HttpPut("services/{id:long}")]
    public async Task<IActionResult> UpdateService(long id, UpdateSecretarySaleServiceCommand command, CancellationToken cancellationToken)
    {
        command.Id = id;
        return Write(await commands.DispatchAsync(command, cancellationToken));
    }

    [HttpPatch("services/{id:long}/status")]
    public async Task<IActionResult> SetServiceStatus(long id, SetSecretarySaleServiceStatusCommand command, CancellationToken cancellationToken)
    {
        command.Id = id;
        return Write(await commands.DispatchAsync(command, cancellationToken));
    }

    [HttpGet("sales")]
    public async Task<IActionResult> Sales([FromQuery] GetAdminSecretarySalesQuery query, CancellationToken cancellationToken) =>
        Ok(await queries.DispatchAsync(query, cancellationToken));

    [HttpPost("sales/{id:long}/approve")]
    public async Task<IActionResult> Approve(long id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var adminUserId)) return Unauthorized();
        return Write(await commands.DispatchAsync(new ApproveSecretarySaleCommand { SaleId = id, AdminUserId = adminUserId }, cancellationToken));
    }

    [HttpPost("sales/{id:long}/reject")]
    public async Task<IActionResult> Reject(long id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var adminUserId)) return Unauthorized();
        return Write(await commands.DispatchAsync(new RejectSecretarySaleCommand { SaleId = id, AdminUserId = adminUserId }, cancellationToken));
    }

    private IActionResult Write<T>(Result<T> result) => result.IsSuccess ? Ok(result) : BadRequest(result);
    private IActionResult Write(Result result) => result.IsSuccess ? Ok(result) : Conflict(result);
    private bool TryGetCurrentUserId(out Guid id) => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("userId") ?? User.FindFirstValue("Id"), out id);
}
