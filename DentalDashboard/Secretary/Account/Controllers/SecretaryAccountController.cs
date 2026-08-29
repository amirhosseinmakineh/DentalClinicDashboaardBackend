using System.Security.Claims;
using DentalDashboard.ApplicationService.Contract.Secretary.Account.Commands;
using DentalDashboard.ApplicationService.Contract.Secretary.Account.Queries;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FluentValidation;

namespace DentalDashboard.Secretary.Account.Controllers;

[ApiController]
[Authorize]
[Route("api/secretary/account")]
public sealed class SecretaryAccountController : ControllerBase
{
    private readonly ICommandDispatcher commandDispatcher;
    private readonly IQueryDispatcher queryDispatcher;
    private readonly IValidator<CreateSecretaryFinancialTransactionCommand> validator;

    public SecretaryAccountController(
        ICommandDispatcher commandDispatcher,
        IQueryDispatcher queryDispatcher,
        IValidator<CreateSecretaryFinancialTransactionCommand> validator)
    {
        this.commandDispatcher = commandDispatcher;
        this.queryDispatcher = queryDispatcher;
        this.validator = validator;
    }

    [HttpPost("financial-transactions")]
    public async Task<IActionResult> CreateTransaction(CreateSecretaryFinancialTransactionCommand command, CancellationToken cancellationToken)
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
    public async Task<IActionResult> GetTransactions([FromQuery] GetSecretaryFinancialTransactionsQuery query, CancellationToken cancellationToken)
    {
        return Ok(await queryDispatcher.DispatchAsync(query, cancellationToken));
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

    [HttpGet("expense-categories")]
    public async Task<IActionResult> GetExpenseCategories(CancellationToken cancellationToken)
    {
        return Ok(await queryDispatcher.DispatchAsync(new GetSecretaryExpenseCategoriesQuery(), cancellationToken));
    }

    private bool TryGetCurrentUserId(out Guid userId)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                    User.FindFirstValue("userId") ??
                    User.FindFirstValue("Id");
        return Guid.TryParse(claim, out userId);
    }
}
