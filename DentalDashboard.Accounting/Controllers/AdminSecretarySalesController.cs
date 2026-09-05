using DentalDashboard.Accounting.Contracts.SecretarySales.Commands;
using DentalDashboard.Accounting.Contracts.SecretarySales.Queries;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalDashboard.Accounting.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/secretary-sales")]
public sealed class AdminSecretarySalesController(ICommandDispatcher commands, IQueryDispatcher queries)
    : AccountingControllerBase
{
    [HttpGet("services")]
    public async Task<IActionResult> Services(
        [FromQuery] GetSecretarySaleServicesQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await queries.DispatchAsync(query, cancellationToken));
    }

    [HttpPost("services")]
    public async Task<IActionResult> CreateService(
        CreateSecretarySaleServiceCommand command,
        CancellationToken cancellationToken)
    {
        return WriteResult(await commands.DispatchAsync(command, cancellationToken));
    }

    [HttpPut("services/{id:long}")]
    public async Task<IActionResult> UpdateService(long id, UpdateSecretarySaleServiceCommand command, CancellationToken cancellationToken)
    {
        command.Id = id;
        return WriteResult(await commands.DispatchAsync(command, cancellationToken));
    }

    [HttpPatch("services/{id:long}/status")]
    public async Task<IActionResult> SetServiceStatus(long id, SetSecretarySaleServiceStatusCommand command, CancellationToken cancellationToken)
    {
        command.Id = id;
        return WriteResult(await commands.DispatchAsync(command, cancellationToken));
    }

    [HttpGet("sales")]
    public async Task<IActionResult> Sales(
        [FromQuery] GetAdminSecretarySalesQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await queries.DispatchAsync(query, cancellationToken));
    }

    [HttpPost("sales/{id:long}/approve")]
    public async Task<IActionResult> Approve(long id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var adminUserId))
        {
            return Unauthorized();
        }

        return WriteResult(await commands.DispatchAsync(
            new ApproveSecretarySaleCommand
            {
                SaleId = id,
                AdminUserId = adminUserId
            },
            cancellationToken));
    }

    [HttpPost("sales/{id:long}/reject")]
    public async Task<IActionResult> Reject(long id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var adminUserId))
        {
            return Unauthorized();
        }

        return WriteResult(await commands.DispatchAsync(
            new RejectSecretarySaleCommand
            {
                SaleId = id,
                AdminUserId = adminUserId
            },
            cancellationToken));
    }
}
