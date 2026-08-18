using Microsoft.AspNetCore.Mvc;

namespace DentalDashboard.Middleware;

public class ApiExceptionHandlingMiddleware
{
    private readonly RequestDelegate next;
    private readonly ILogger<ApiExceptionHandlingMiddleware> logger;

    public ApiExceptionHandlingMiddleware(RequestDelegate next, ILogger<ApiExceptionHandlingMiddleware> logger)
    {
        this.next = next;
        this.logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            var (status, title) = exception switch
            {
                ArgumentException => (StatusCodes.Status400BadRequest, "Validation failed"),
                KeyNotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
                UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "Access denied"),
                InvalidOperationException => (StatusCodes.Status409Conflict, "Operation rejected"),
                _ => (StatusCodes.Status500InternalServerError, "Unexpected error")
            };
            if (status == StatusCodes.Status500InternalServerError)
                logger.LogError(exception, "Unhandled API exception");
            else
                logger.LogWarning(exception, "API request failed with status {StatusCode}", status);

            context.Response.StatusCode = status;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = status == StatusCodes.Status500InternalServerError
                    ? "An unexpected error occurred." : exception.Message
            });
        }
    }
}
