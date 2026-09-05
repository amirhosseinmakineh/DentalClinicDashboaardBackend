using DentalDashboard.Accounting.Contracts.Commands;
using DentalDashboard.Accounting.Contracts.Queries;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using DentalDashboard.Framwork.Domain;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalDashboard.Accounting.Controllers;

[ApiController]
[Authorize]
[Route("api/secretary/expense-categories")]
public sealed class ExpenseCategoryController : AccountingControllerBase
{
    private readonly ICommandDispatcher commandDispatcher;
    private readonly IQueryDispatcher queryDispatcher;
    private readonly IValidator<CreateExpenseCommand> createValidator;
    private readonly IValidator<UpdateExpenseCategoryCommand> updateValidator;

    public ExpenseCategoryController(
        ICommandDispatcher commandDispatcher,
        IQueryDispatcher queryDispatcher,
        IValidator<CreateExpenseCommand> createValidator,
        IValidator<UpdateExpenseCategoryCommand> updateValidator)
    {
        this.commandDispatcher = commandDispatcher;
        this.queryDispatcher = queryDispatcher;
        this.createValidator = createValidator;
        this.updateValidator = updateValidator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await queryDispatcher.DispatchAsync(
            new GetExpenseCategoriesQuery(), cancellationToken));
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
    {
        var result = await queryDispatcher.DispatchAsync(
            new GetExpenseCategoryDetailsQuery(id), cancellationToken);
        return result is null
            ? NotFound(Result.Failure("دسته‌بندی هزینه یافت نشد"))
            : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        CreateExpenseCommand command,
        CancellationToken cancellationToken)
    {
        var validationFailure = await ValidateCommandAsync(
            createValidator, command, cancellationToken);
        if (validationFailure is not null)
        {
            return validationFailure;
        }

        var result = await commandDispatcher.DispatchAsync(command, cancellationToken);
        return WriteResult(result);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(
        long id,
        UpdateExpenseCategoryCommand command,
        CancellationToken cancellationToken)
    {
        command.Id = id;
        var validationFailure = await ValidateCommandAsync(
            updateValidator, command, cancellationToken);
        if (validationFailure is not null)
        {
            return validationFailure;
        }

        var result = await commandDispatcher.DispatchAsync(command, cancellationToken);
        return WriteResult(result);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return BadRequest(Result.Failure("شناسه دسته‌بندی هزینه معتبر نیست"));
        }

        var result = await commandDispatcher.DispatchAsync(
            new DeleteExpenseCategoryCommand(id), cancellationToken);
        return WriteResult(result);
    }
}
