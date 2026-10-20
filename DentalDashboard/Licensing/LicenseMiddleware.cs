using DentalDashboard.Framwork.Licensing;

namespace DentalDashboard.Licensing;

public sealed class LicenseMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ILicenseAccess license)
    {
        var path = context.Request.Path;

        if (path == "/healthz" || path == "/readyz" || path == "/api/license/status")
        {
            await next(context);
            return;
        }

        var status = license.Status;

        if (!status.IsValid)
        {
            await WriteUnavailableAsync(context, status.Reason);
            return;
        }

        try
        {
            await next(context);
        }
        catch (LicenseUnavailableException exception) when (!context.Response.HasStarted)
        {
            await WriteUnavailableAsync(context, exception.Message);
        }
    }

    private static Task WriteUnavailableAsync(HttpContext context, string? reason)
    {
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        context.Response.Headers.RetryAfter = "300";
        context.Response.Headers.CacheControl = "no-store";
        return context.Response.WriteAsJsonAsync(new
        {
            title = "لایسنس کلینیک در دسترس یا معتبر نیست",
            status = StatusCodes.Status503ServiceUnavailable,
            code = reason
        });
    }
}
