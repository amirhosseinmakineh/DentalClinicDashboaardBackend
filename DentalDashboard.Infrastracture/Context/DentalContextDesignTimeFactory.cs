using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace DentalDashboard.Infrastracture.Context;

/// <summary>Uses the same configuration sources as the web application for EF design-time operations.</summary>
public sealed class DentalContextDesignTimeFactory : IDesignTimeDbContextFactory<DentalContext>
{
    public DentalContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? "Development";

        var startupDirectory = Path.Combine(Directory.GetCurrentDirectory(), "DentalDashboard");
        if (!File.Exists(Path.Combine(startupDirectory, "appsettings.json")))
        {
            startupDirectory = Directory.GetCurrentDirectory();
        }

        var configuration = new ConfigurationBuilder()
            .SetBasePath(startupDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connection = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connection))
        {
            throw new InvalidOperationException(
                "No DefaultConnection configured for EF migrations. Check the startup project's appsettings files.");
        }

        return new DentalContext(
            new DbContextOptionsBuilder<DentalContext>()
                .UseSqlServer(connection)
                .Options);
    }
}
