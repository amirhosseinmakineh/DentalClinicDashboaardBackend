using DentalDashboard.Infrastracture.Migrations;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using System.Reflection;

namespace DentalDashboard.Tests;

public class ConsultantRoleEvaluationMigrationTests
{
    [Fact]
    public void Migration_backfills_all_roles_and_rollback_removes_feature_schema()
    {
        var migration = new AddConsultantRoleEvaluations();
        var up = BuildOperations(migration, "Up");
        var sql = Assert.Single(up.OfType<SqlOperation>()).Sql;

        Assert.Contains("SYSUTCDATETIME()", sql);
        Assert.Contains("WHEN 3 THEN 7", sql);
        Assert.Contains("ELSE 10", sql);
        Assert.Contains("COALESCE([RoleStartedAt], @Baseline)", sql);
        Assert.Contains("[NextRoleEvaluationAt]", sql);

        var down = BuildOperations(migration, "Down");
        Assert.Contains(down.OfType<DropTableOperation>(), x => x.Name == "ConsultantRoleEvaluations");
        Assert.Contains(down.OfType<DropColumnOperation>(), x => x.Name == "RoleStartedAt");
        Assert.Contains(down.OfType<DropColumnOperation>(), x => x.Name == "NextRoleEvaluationAt");
    }

    private static IReadOnlyList<MigrationOperation> BuildOperations(Migration migration, string methodName)
    {
        var builder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");
        typeof(AddConsultantRoleEvaluations)
            .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(migration, new object[] { builder });
        return builder.Operations;
    }
}
