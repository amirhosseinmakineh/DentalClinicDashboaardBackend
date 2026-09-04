using System.Security.Claims;
using DentalDashboard.Framwork.Domain;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DentalDashboard.Framwork.Web;

public abstract class DashboardApiControllerBase : ControllerBase
{
    protected bool TryGetCurrentUserId(out Guid userId)
    {
        var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                         User.FindFirstValue("userId") ??
                         User.FindFirstValue("Id");

        return Guid.TryParse(claimValue, out userId);
    }

    protected IActionResult WriteResult(
        Result result,
        int failureStatusCode = StatusCodes.Status409Conflict)
    {
        return result.IsSuccess
            ? Ok(result)
            : StatusCode(failureStatusCode, result);
    }

    protected IActionResult WriteResult<T>(
        Result<T> result,
        int failureStatusCode = StatusCodes.Status400BadRequest)
    {
        return result.IsSuccess
            ? Ok(result)
            : StatusCode(failureStatusCode, result);
    }

    protected async Task<IActionResult?> ValidateCommandAsync<TCommand>(
        IValidator<TCommand> validator,
        TCommand command,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(
            command,
            cancellationToken);

        if (validationResult.IsValid)
        {
            return null;
        }

        var errorMessage = string.Join(
            " | ",
            validationResult.Errors.Select(error => error.ErrorMessage));

        return BadRequest(Result.Failure(errorMessage));
    }
}
