namespace DentalDashboard.Middleware;

/// <summary>
/// Treats a disconnected client or an explicitly aborted HTTP request as a
/// completed cancellation instead of allowing it to reach the exception pipeline.
/// </summary>
public sealed class RequestCancellationMiddleware(RequestDelegate next)
{
    private const int ClientClosedRequestStatusCode = 499;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            if (!context.Response.HasStarted)
            {
                context.Response.Clear();
                context.Response.StatusCode = ClientClosedRequestStatusCode;
            }
        }
    }
}
