using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DentalDashboard.Infrastracture.Context;

/// <summary>Scaffold migrations without starting the web app or contacting a live database.</summary>
public sealed class DentalContextDesignTimeFactory : IDesignTimeDbContextFactory<DentalContext>
{
    public DentalContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Server=localhost;Database=DentalDashboardDesignTime;Integrated Security=true;TrustServerCertificate=true";
        return new DentalContext(new DbContextOptionsBuilder<DentalContext>().UseSqlServer(connection).Options);
    }
}
