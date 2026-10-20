using DentalDashboard.Framwork.Licensing;
using Microsoft.AspNetCore.SignalR;

namespace DentalDashboard.Licensing;

public sealed class LicenseHubFilter(ILicenseAccess license) : IHubFilter
{
    public async ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        Check(invocationContext.Context);
        return await next(invocationContext);
    }

    public async Task OnConnectedAsync(
        HubLifetimeContext context,
        Func<HubLifetimeContext, Task> next)
    {
        Check(context.Context);
        await next(context);
    }

    private void Check(HubCallerContext context)
    {
        if (!license.Status.IsValid)
        {
            context.Abort();
            throw new HubException("license_unavailable");
        }
    }
}
