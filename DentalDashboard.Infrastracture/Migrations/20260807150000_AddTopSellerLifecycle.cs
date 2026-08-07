using DentalDashboard.Infrastracture.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalDashboard.Infrastracture.Migrations;

[DbContext(typeof(DentalContext))]
[Migration("20260807150000_AddTopSellerLifecycle")]
public sealed class AddTopSellerLifecycle : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>("TopSellerStartedAt", "ConsultantProfiles", "datetime2", nullable: true);
        migrationBuilder.AddColumn<DateTime>("TopSellerLastEvaluatedPeriodStart", "ConsultantProfiles", "datetime2", nullable: true);
        migrationBuilder.AddColumn<DateTime>("TopSellerLastEvaluatedAt", "ConsultantProfiles", "datetime2", nullable: true);
        migrationBuilder.AddColumn<byte>("TopSellerRewardLevel", "ConsultantProfiles", "tinyint", nullable: false, defaultValue: (byte)0);
        migrationBuilder.Sql(
            "UPDATE ConsultantProfiles SET TopSellerStartedAt = " +
            "DATEADD(MINUTE, -210, CAST(DATEADD(MINUTE, 210, GETUTCDATE()) AS date)) " +
            "WHERE ConsultantLevel = 3 AND TopSellerStartedAt IS NULL");
        migrationBuilder.CreateIndex(
            name: "IX_ConsultantProfiles_ConsultantLevel_TopSellerStartedAt_TopSellerLastEvaluatedPeriodStart",
            table: "ConsultantProfiles",
            columns: new[] { "ConsultantLevel", "TopSellerStartedAt", "TopSellerLastEvaluatedPeriodStart" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            "IX_ConsultantProfiles_ConsultantLevel_TopSellerStartedAt_TopSellerLastEvaluatedPeriodStart",
            "ConsultantProfiles");
        migrationBuilder.DropColumn("TopSellerStartedAt", "ConsultantProfiles");
        migrationBuilder.DropColumn("TopSellerLastEvaluatedPeriodStart", "ConsultantProfiles");
        migrationBuilder.DropColumn("TopSellerLastEvaluatedAt", "ConsultantProfiles");
        migrationBuilder.DropColumn("TopSellerRewardLevel", "ConsultantProfiles");
    }
}
