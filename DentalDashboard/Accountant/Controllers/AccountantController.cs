using System.Security.Claims;
using DentalDashboard.ApplicationService.Contract.Accountant.Commands;
using DentalDashboard.ApplicationService.Contract.Accountant.Queries;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FluentValidation;

namespace DentalDashboard.Accountant.Controllers;

[ApiController]
[Authorize]
[Route("api/secretary/account")]
[Route("api/accountant")]
public sealed class AccountantController : ControllerBase
{
    private readonly ICommandDispatcher commandDispatcher;
    private readonly IQueryDispatcher queryDispatcher;
    private readonly IValidator<CreateFinancialTransactionCommand> validator;
    private readonly IValidator<UpdateFinancialTransactionCommand> updateValidator;

    public AccountantController(
        ICommandDispatcher commandDispatcher,
        IQueryDispatcher queryDispatcher,
        IValidator<CreateFinancialTransactionCommand> validator,
        IValidator<UpdateFinancialTransactionCommand> updateValidator)
    {
        this.commandDispatcher = commandDispatcher;
        this.queryDispatcher = queryDispatcher;
        this.validator = validator;
        this.updateValidator = updateValidator;
    }

    [HttpPost("financial-transactions")]
    public async Task<IActionResult> CreateTransaction(CreateFinancialTransactionCommand command, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        command.CreatedByUserId = userId;
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            var message = string.Join(" | ", validationResult.Errors.Select(x => x.ErrorMessage));
            return BadRequest(Result.Failure(message));
        }

        var result = await commandDispatcher.DispatchAsync(command, cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpGet("financial-transactions")]
    public async Task<IActionResult> GetTransactions([FromQuery] GetFinancialTransactionsQuery query, CancellationToken cancellationToken)
    {
        return Ok(await queryDispatcher.DispatchAsync(query, cancellationToken));
    }

    [HttpPut("financial-transactions/{id:long}")]
    public async Task<IActionResult> UpdateTransaction(
        long id,
        UpdateFinancialTransactionCommand command,
        CancellationToken cancellationToken)
    {
        command.Id = id;
        var validationResult = await updateValidator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            var message = string.Join(" | ", validationResult.Errors.Select(x => x.ErrorMessage));
            return BadRequest(Result.Failure(message));
        }

        var result = await commandDispatcher.DispatchAsync(command, cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpDelete("financial-transactions/{id:long}")]
    public async Task<IActionResult> DeleteTransaction(long id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return BadRequest(Result.Failure("شناسه تراکنش معتبر نیست"));
        }

        var result = await commandDispatcher.DispatchAsync(
            new DeleteFinancialTransactionCommand(id), cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpGet("financial-transactions/summary")]
    public async Task<IActionResult> GetSummary([FromQuery] GetFinancialSummaryQuery query, CancellationToken cancellationToken)
    {
        return Ok(await queryDispatcher.DispatchAsync(query, cancellationToken));
    }

    [HttpGet("financial-transactions/{id:long}")]
    public async Task<IActionResult> GetTransaction(long id, CancellationToken cancellationToken)
    {
        var result = await queryDispatcher.DispatchAsync(
            new GetFinancialTransactionDetailsQuery { Id = id },
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
            new GetFinancialTransactionReceiptQuery { Id = id },
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
        return Ok(await queryDispatcher.DispatchAsync(new GetAvailableExpenseCategoriesQuery(), cancellationToken));
    }

    private bool TryGetCurrentUserId(out Guid userId)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                    User.FindFirstValue("userId") ??
                    User.FindFirstValue("Id");
        return Guid.TryParse(claim, out userId);
    }
}
