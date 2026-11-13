using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System.Text.Json;

namespace DentalDashboard.Infrastracture.Context;

/// <summary>Resolves the same DefaultConnection configured for the web application.</summary>
public sealed class DentalContextDesignTimeFactory : IDesignTimeDbContextFactory<DentalContext>
{
    public DentalContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? "Development";

        var workingDirectory = Directory.GetCurrentDirectory();
        var startupDirectory = File.Exists(Path.Combine(workingDirectory, "DentalDashboard", "appsettings.json"))
            ? Path.Combine(workingDirectory, "DentalDashboard")
            : workingDirectory;

        string? connection = null;
        foreach (var fileName in new[] { "appsettings.json", $"appsettings.{environment}.json" })
        {
            var filePath = Path.Combine(startupDirectory, fileName);
            if (!File.Exists(filePath)) continue;

            using var document = JsonDocument.Parse(File.ReadAllText(filePath));
            if (document.RootElement.TryGetProperty("ConnectionStrings", out var connections) &&
                connections.TryGetProperty("DefaultConnection", out var configuredConnection) &&
                configuredConnection.ValueKind == JsonValueKind.String)
            {
                connection = configuredConnection.GetString();
            }
        }

        connection = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") ?? connection;
        if (string.IsNullOrWhiteSpace(connection))
        {
            throw new InvalidOperationException(
                "DefaultConnection was not found in the startup appsettings files or environment variables.");
        }

        return new DentalContext(
            new DbContextOptionsBuilder<DentalContext>()
                .UseSqlServer(connection)
                .Options);
    }
}
