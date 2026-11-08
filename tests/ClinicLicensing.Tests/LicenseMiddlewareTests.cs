using DentalDashboard.Framwork.Licensing;
using DentalDashboard.Licensing;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace DentalDashboard.Tests;

public sealed class LicenseMiddlewareTests
{
    [Theory]
    [InlineData("/api/patients")]
    [InlineData("/api/auth/login")]
    [InlineData("/hubs/reservations")]
    [InlineData("/uploads/document.pdf")]
    [InlineData("/api/license/status/extra")]
    [InlineData("/healthz/extra")]
    public async Task InvalidLicenseStopsBusinessRequests(string path)
    {
        var called = false;
        var middleware = new LicenseMiddleware(context =>
        {
            called = true;
            return Task.CompletedTask;
        });
        var context = Context(path);
        await middleware.InvokeAsync(context, new TestAccess(false));
        Assert.False(called);
        Assert.Equal(503, context.Response.StatusCode);
        Assert.Equal("300", context.Response.Headers.RetryAfter);
    }

    [Theory]
    [InlineData("/healthz")]
    [InlineData("/readyz")]
    [InlineData("/api/license/status")]
    public async Task HealthAndStatusRemainAvailable(string path)
    {
        var called = false;
        var middleware = new LicenseMiddleware(context =>
        {
            called = true;
            return Task.CompletedTask;
        });
        await middleware.InvokeAsync(Context(path), new TestAccess(false));
        Assert.True(called);
    }

    [Fact]
    public async Task ValidLicensePermitsTheRequest()
    {
        var called = false;
        var middleware = new LicenseMiddleware(context =>
        {
            called = true;
            return Task.CompletedTask;
        });
        await middleware.InvokeAsync(Context("/api/patients"), new TestAccess(true));
        Assert.True(called);
    }

    [Fact]
    public async Task RevocationBetweenMiddlewareAndDispatchReturns503()
    {
        var middleware = new LicenseMiddleware(context =>
            throw new LicenseUnavailableException("license_disabled"));
        var context = Context("/api/patients");
        await middleware.InvokeAsync(context, new TestAccess(true));
        Assert.Equal(503, context.Response.StatusCode);
    }

    private static DefaultHttpContext Context(string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();
        return context;
    }

    private sealed class TestAccess(bool valid) : ILicenseAccess
    {
        public LicenseValidationResult Status => valid
            ? LicenseValidationResult.Valid(DateTimeOffset.UtcNow.AddHours(1))
            : LicenseValidationResult.Invalid("license_disabled");

        public void EnsureAllowed()
        {
            if (!valid)
            {
                throw new LicenseUnavailableException("license_disabled");
            }
        }
    }
}
