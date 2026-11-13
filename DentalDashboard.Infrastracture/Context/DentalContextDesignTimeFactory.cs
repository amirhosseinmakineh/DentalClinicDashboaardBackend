using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DentalDashboard.Infrastracture.Context;

/// <summary>
/// Design-time database selection must be explicit. Never silently migrate a
/// separate placeholder database when running Update-Database.
/// </summary>
public sealed class DentalContextDesignTimeFactory : IDesignTimeDbContextFactory<DentalContext>
{
    public DentalContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (string.IsNullOrWhiteSpace(connection))
        {
            throw new InvalidOperationException(
                "Set ConnectionStrings__DefaultConnection to the target SQL Server connection string " +
                "in the current terminal/Package Manager Console before running EF Core migrations. " +
                "The design-time factory will not select a database implicitly.");
        }

        return new DentalContext(
            new DbContextOptionsBuilder<DentalContext>()
                .UseSqlServer(connection)
                .Options);
    }
}
