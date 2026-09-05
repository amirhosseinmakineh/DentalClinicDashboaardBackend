using DentalDashboard.Accounting.Contracts.SecretarySales.Commands;
using DentalDashboard.Accounting.Contracts.SecretarySales.Queries;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalDashboard.Accounting.Controllers;

[ApiController]
[Authorize(Roles = "Secretary")]
[Route("api/secretary/account/sales")]
public sealed class SecretarySalesController(ICommandDispatcher commands, IQueryDispatcher queries)
    : AccountingControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateSecretarySaleCommand command, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var secretaryUserId))
        {
            return Unauthorized();
        }

        command.SecretaryUserId = secretaryUserId;
        var result = await commands.DispatchAsync(command, cancellationToken);
        return WriteResult(result);
    }

    [HttpGet]
    public async Task<IActionResult> Sales([FromQuery] GetSecretarySalesQuery query, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var secretaryUserId))
        {
            return Unauthorized();
        }

        query.SecretaryUserId = secretaryUserId;
        return Ok(await queries.DispatchAsync(query, cancellationToken));
    }

    [HttpGet("services")]
    public async Task<IActionResult> Services(CancellationToken cancellationToken)
    {
        return Ok(await queries.DispatchAsync(
            new GetActiveSecretarySaleServicesQuery(),
            cancellationToken));
    }

    [HttpGet("patients")]
    public async Task<IActionResult> Patients(
        [FromQuery] SearchSecretarySalePatientsQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await queries.DispatchAsync(query, cancellationToken));
    }

    [HttpGet("wallet")]
    public async Task<IActionResult> Wallet(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var secretaryUserId))
        {
            return Unauthorized();
        }

        return Ok(await queries.DispatchAsync(
            new GetSecretaryWalletQuery
            {
                SecretaryUserId = secretaryUserId
            },
            cancellationToken));
    }

    [HttpGet("wallet/transactions")]
    public async Task<IActionResult> WalletTransactions([FromQuery] GetSecretaryWalletTransactionsQuery query, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var secretaryUserId))
        {
            return Unauthorized();
        }

        query.SecretaryUserId = secretaryUserId;
        return Ok(await queries.DispatchAsync(query, cancellationToken));
    }
}
