using DentalDashboard.Accounting.Contracts.Commands;
using DentalDashboard.Accounting.Contracts.Queries;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FluentValidation;

namespace DentalDashboard.Accounting.Controllers;

[ApiController]
[Authorize]
[Route("api/secretary/account")]
public sealed class SecretaryAccountController : AccountingControllerBase
{
    private readonly ICommandDispatcher commandDispatcher;
    private readonly IQueryDispatcher queryDispatcher;
    private readonly IValidator<CreateSecretaryFinancialTransactionCommand> validator;
    private readonly IValidator<UpdateSecretaryFinancialTransactionCommand> updateValidator;

    public SecretaryAccountController(
        ICommandDispatcher commandDispatcher,
        IQueryDispatcher queryDispatcher,
        IValidator<CreateSecretaryFinancialTransactionCommand> validator,
        IValidator<UpdateSecretaryFinancialTransactionCommand> updateValidator)
    {
        this.commandDispatcher = commandDispatcher;
        this.queryDispatcher = queryDispatcher;
        this.validator = validator;
        this.updateValidator = updateValidator;
    }

    [HttpPost("financial-transactions")]
    public async Task<IActionResult> CreateTransaction(CreateSecretaryFinancialTransactionCommand command, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        command.CreatedByUserId = userId;
        var validationFailure = await ValidateCommandAsync(
            validator,
            command,
            cancellationToken);
        if (validationFailure is not null)
        {
            return validationFailure;
        }

        var result = await commandDispatcher.DispatchAsync(command, cancellationToken);
        return WriteResult(result);
    }

    [HttpGet("financial-transactions")]
    public async Task<IActionResult> GetTransactions([FromQuery] GetSecretaryFinancialTransactionsQuery query, CancellationToken cancellationToken)
    {
        return Ok(await queryDispatcher.DispatchAsync(query, cancellationToken));
    }

    [HttpPut("financial-transactions/{id:long}")]
    public async Task<IActionResult> UpdateTransaction(
        long id,
        UpdateSecretaryFinancialTransactionCommand command,
        CancellationToken cancellationToken)
    {
        command.Id = id;
        var validationFailure = await ValidateCommandAsync(
            updateValidator,
            command,
            cancellationToken);
        if (validationFailure is not null)
        {
            return validationFailure;
        }

        var result = await commandDispatcher.DispatchAsync(command, cancellationToken);
        return WriteResult(result);
    }

    [HttpDelete("financial-transactions/{id:long}")]
    public async Task<IActionResult> DeleteTransaction(long id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return BadRequest(Result.Failure("شناسه تراکنش معتبر نیست"));
        }

        var result = await commandDispatcher.DispatchAsync(
            new DeleteSecretaryFinancialTransactionCommand(id), cancellationToken);
        return WriteResult(result);
    }

    [HttpGet("financial-transactions/summary")]
    public async Task<IActionResult> GetSummary([FromQuery] GetSecretaryFinancialSummaryQuery query, CancellationToken cancellationToken)
    {
        return Ok(await queryDispatcher.DispatchAsync(query, cancellationToken));
    }

    [HttpGet("financial-transactions/{id:long}")]
    public async Task<IActionResult> GetTransaction(long id, CancellationToken cancellationToken)
    {
        var result = await queryDispatcher.DispatchAsync(
            new GetSecretaryFinancialTransactionDetailsQuery { Id = id },
            cancellationToken);
        return result is null ? NotFound(Result.Failure("تراکنش مالی یافت نشد")) : Ok(result);
    }

    [HttpGet("financial-transactions/{id:long}/receipt")]
    public async Task<IActionResult> GetTransactionReceipt(long id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return BadRequest(Result.Failure("شناسه تراکنش معتبر نیست"));
        }

        var receipt = await queryDispatcher.DispatchAsync(
            new GetSecretaryFinancialTransactionReceiptQuery { Id = id },
            cancellationToken);

        if (receipt is null)
        {
            return NotFound(Result.Failure("تراکنش مالی یافت نشد"));
        }

        Response.Headers.ContentDisposition = $"inline; filename=\"{receipt.FileName}\"";

        return File(receipt.Content, receipt.ContentType);
    }

    [HttpGet("expense-categories")]
    public async Task<IActionResult> GetExpenseCategories(CancellationToken cancellationToken)
    {
        return Ok(await queryDispatcher.DispatchAsync(new GetSecretaryExpenseCategoriesQuery(), cancellationToken));
    }
}
