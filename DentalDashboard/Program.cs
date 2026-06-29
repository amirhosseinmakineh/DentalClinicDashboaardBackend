using DentalDashboard.ApplicationService;
using DentalDashboard.BackgroundServices;
using DentalDashboard.Domain;
using DentalDashboard.Infrastracture.Registration;
using DentalDashboard.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ====================================
// Services
// ====================================

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();

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
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
        NameClaimType = System.Security.Claims.ClaimTypes.Name,
        RoleClaimType = System.Security.Claims.ClaimTypes.Role
    };
});

builder.Services.AddAuthorization();

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
        policy
            .WithOrigins(
                "https://drsaeedmoghadam.com",
                "https://www.drsaeedmoghadam.com",
                "http://localhost:4200",
                "https://drmoghadam.runflare.run",
                "http://drsaeedmoghadam.com"
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
builder.Services.AddApplicationServices();

builder.Services.AddDomainServices();

builder.Services.AddSecurityServices();

builder.Services.AddScoped<DentalDashboard.Services.LeadCallReportExportService>();

builder.Services.AddHostedService<LeadAssignmentBackgroundService>();

builder.Services.AddInfrastructure(
    builder.Configuration);

// ====================================
// App
// ====================================

var app = builder.Build();

// ====================================
// Middleware
// ====================================

app.UseHttpsRedirection();

//app.UseCors("CorsPolicy");
app.UseCors("FrontendCors");

app.UseAuthentication();

app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();