using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace DentalDashboard.Controllers;

public static class ConsultationRateLimiting
{
    public static IServiceCollection AddConsultationRateLimiting(this IServiceCollection services) =>
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("consultation", context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5, Window = TimeSpan.FromMinutes(5), QueueLimit = 0
                }));
        });
}
