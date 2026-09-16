using DentalDashboard.ApplicationService;
using DentalDashboard.ApplicationService.Contract.IServices;
using DentalDashboard.ApplicationService.Services;
using DentalDashboard.BackgroundServices;
using DentalDashboard.Domain;
using DentalDashboard.Infrastracture.Context;
using DentalDashboard.Infrastracture.Registration;
using DentalDashboard.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using DentalDashboard.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// ====================================
// Services
// ====================================

builder.Services.AddControllers();
builder.Services.AddSignalR();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var jwtSecretKey = jwtSettings["SecretKey"];
if (string.IsNullOrWhiteSpace(jwtSecretKey))
{
    throw new InvalidOperationException("JwtSettings:SecretKey is not configured.");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ClockSkew = TimeSpan.FromMinutes(1),
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
        NameClaimType = System.Security.Claims.ClaimTypes.Name,
        RoleClaimType = System.Security.Claims.ClaimTypes.Role
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            if (!string.IsNullOrEmpty(accessToken) &&
                context.HttpContext.Request.Path.StartsWithSegments("/hubs/reservations"))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

//builder.Services.AddCors(options =>
//{
//    options.AddPolicy("CorsPolicy", policy =>
//    {
//        policy
//            .WithOrigins("http://localhost:4200")
//            .AllowAnyHeader()
//            .AllowAnyMethod();
//    });
//});
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendCors", policy =>
    {
        var allowedOrigins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>()?
            .Where(origin => !string.IsNullOrWhiteSpace(origin))
            .Select(origin => origin.Trim().TrimEnd('/'))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? new List<string>
        {
            "https://drsaeedmoghadam.com",
            "https://www.drsaeedmoghadam.com",
            "https://drmoghadam.runflare.run",
            "https://stage.drsaeedmoghadam.com"
        };

        if (builder.Environment.IsDevelopment())
            allowedOrigins.Add("http://localhost:4200");

        policy
            .WithOrigins(allowedOrigins.ToArray())
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
builder.Services.AddApplicationServices();

builder.Services.AddDomainServices();

builder.Services.AddSecurityServices();

builder.Services.AddScoped<DentalDashboard.Services.LeadCallReportExportService>();
builder.Services.AddScoped<DentalDashboard.Services.UsersExportService>();
builder.Services.AddScoped<DentalDashboard.Services.LeadsExportService>();
builder.Services.AddScoped<DentalDashboard.Services.ConsultantsExportService>();
builder.Services.AddScoped<DentalDashboard.Services.ConsultantsDailySummaryService>();
builder.Services.AddScoped<DentalDashboard.Services.ReservationsExportService>();
builder.Services.AddScoped<DentalDashboard.Services.DailyReservationsReportService>();
builder.Services.AddScoped<DentalDashboard.Services.PatientFinanceAdminReportService>();
builder.Services.AddScoped<ILeadAssignmentLimitService, LeadAssignmentLimitService>();

builder.Services.Configure<HostOptions>(options =>
{
    options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
});

//builder.Services.AddHostedService<LeadAssignmentBackgroundService>();
builder.Services.AddHostedService<TestConsultantLeadAssignmentBackgroundService>();
builder.Services.AddHostedService<SellerConsultantLeadAssignmentBackgroundService>();
builder.Services.AddHostedService<TopSellerConsultantLeadAssignmentBackgroundService>();
builder.Services.AddHostedService<AddLeadBackgroundService>();

builder.Services.AddInfrastructure(
    builder.Configuration);

// ====================================
// App
// ====================================

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<DentalContext>();
    var migrationLogger = scope.ServiceProvider
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("DatabaseMigration");

    try
    {
        migrationLogger.LogInformation("Applying EF Core database migrations...");
        dbContext.Database.Migrate();
        migrationLogger.LogInformation("Database migrations applied successfully.");
    }
    catch (Exception ex)
    {
        migrationLogger.LogCritical(
            ex,
            "Database migration failed. Run docs/sql/2026-07-06-user-presence-logs-fix.sql and redeploy the latest backend build.");
        throw;
    }
}

// ====================================
// Middleware
// ====================================

app.UseMiddleware<DentalDashboard.Middleware.RequestCancellationMiddleware>();

app.UseExceptionHandler(exceptionHandler =>
{
    exceptionHandler.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(new
        {
            type = "https://httpstatuses.com/500",
            title = "خطای داخلی سرور",
            status = StatusCodes.Status500InternalServerError,
            detail = "در پردازش درخواست خطایی رخ داد"
        });
    });
});

app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["Referrer-Policy"] = "no-referrer";
        context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        return Task.CompletedTask;
    });

    await next();
});

// Authenticated API data must never be served from a browser, CDN or proxy cache.
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api"))
    {
        context.Response.OnStarting(() =>
        {
            context.Response.Headers.CacheControl = "no-store, no-cache, max-age=0, must-revalidate";
            context.Response.Headers.Pragma = "no-cache";
            context.Response.Headers.Expires = "0";
            return Task.CompletedTask;
        });
    }

    await next();
});

//app.UseCors("CorsPolicy");
app.UseCors("FrontendCors");

app.UseAuthentication();

app.UseAuthorization();

app.UseMiddleware<DentalDashboard.Middleware.LastSeenTrackingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI();
}

app.MapControllers();
app.MapHub<ReservationsHub>("/hubs/reservations");
app.MapHealthChecks("/healthz").AllowAnonymous();
app.MapGet("/readyz", async (DentalContext dbContext, CancellationToken cancellationToken) =>
    await dbContext.Database.CanConnectAsync(cancellationToken)
        ? Results.Ok(new { status = "ready" })
        : Results.StatusCode(StatusCodes.Status503ServiceUnavailable))
    .AllowAnonymous();

app.Run();
