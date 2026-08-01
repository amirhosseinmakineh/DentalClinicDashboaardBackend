using System.Collections.Concurrent;
using System.Security.Claims;
using DentalDashboard.Infrastracture.Context;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Middleware;

public class LastSeenTrackingMiddleware
{
    private static readonly ConcurrentDictionary<Guid, DateTime> LastPersistedAt = new();
    private static readonly TimeSpan PersistInterval = TimeSpan.FromMinutes(1);

    private readonly RequestDelegate next;

    public LastSeenTrackingMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    public async Task InvokeAsync(HttpContext context, DentalContext db)
    {
        await next(context);

        if (IsLogoutRequest(context))
            return;

        if (context.User.Identity?.IsAuthenticated != true)
            return;

        var userIdValue = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdValue, out var userId))
            return;

        var now = DateTime.UtcNow;
        if (LastPersistedAt.TryGetValue(userId, out var lastPersisted) &&
            now - lastPersisted < PersistInterval)
        {
            return;
        }

        var updated = await db.Users
            .Where(x => x.Id == userId && !x.IsDeleted)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.LastSeenAt, now)
                .SetProperty(x => x.UpdatedAt, now));

        if (updated > 0)
            LastPersistedAt[userId] = now;
    }

    private static bool IsLogoutRequest(HttpContext context)
    {
        return context.Request.Path.StartsWithSegments(
            "/Auth/Logout",
            StringComparison.OrdinalIgnoreCase);
    }
}
