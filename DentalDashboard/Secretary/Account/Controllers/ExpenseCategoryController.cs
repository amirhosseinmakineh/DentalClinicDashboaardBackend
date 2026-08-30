using System.Security.Claims;
using DentalDashboard.ApplicationService.Contract.Secretary.Account.Commands;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FluentValidation;

namespace DentalDashboard.Secretary.Account.Controllers;

[ApiController]
[Authorize]
[Route("api/secretary/Expense")]
public sealed class ExpenseCategoryController : ControllerBase
{
    private readonly ICommandDispatcher commandDispatcher;
    private readonly IQueryDispatcher queryDispatcher;
    private readonly IValidator<CreateExpenseCommand> validator;

    public ExpenseCategoryController(
    ICommandDispatcher commandDispatcher,
    IQueryDispatcher queryDispatcher,
    IValidator<CreateExpenseCommand> validator)
    {
        this.commandDispatcher = commandDispatcher;
        this.queryDispatcher = queryDispatcher;
        this.validator = validator;
    }
    [HttpPost]
    public async Task<IActionResult> Create(CreateExpenseCommand command,CancellationToken cancellationToken)
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
    private bool TryGetCurrentUserId(out Guid userId)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                    User.FindFirstValue("userId") ??
                    User.FindFirstValue("Id");
        return Guid.TryParse(claim, out userId);
    }


}
