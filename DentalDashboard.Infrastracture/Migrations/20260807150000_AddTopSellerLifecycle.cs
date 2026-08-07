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
        migrationBuilder.AddColumn<DateTime>(
            name: "TopSellerStartedAt",
            table: "ConsultantProfiles",
            type: "datetime2",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "TopSellerLastEvaluatedPeriodStart",
            table: "ConsultantProfiles",
            type: "datetime2",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "TopSellerLastEvaluatedAt",
            table: "ConsultantProfiles",
            type: "datetime2",
            nullable: true);

        migrationBuilder.AddColumn<byte>(
            name: "TopSellerRewardLevel",
            table: "ConsultantProfiles",
            type: "tinyint",
            nullable: false,
            defaultValue: (byte)0);

        migrationBuilder.Sql(@"
            UPDATE ConsultantProfiles
            SET TopSellerStartedAt =
                DATEADD(
                    MINUTE,
                    -210,
                    CAST(
                        CAST(
                            DATEADD(MINUTE, 210, GETUTCDATE())
                            AS date
                        )
                        AS datetime2
                    )
                )
            WHERE ConsultantLevel = 3
              AND TopSellerStartedAt IS NULL;
        ");

        migrationBuilder.CreateIndex(
            name: "IX_ConsultantProfiles_ConsultantLevel_TopSellerStartedAt_TopSellerLastEvaluatedPeriodStart",
            table: "ConsultantProfiles",
            columns: new[]
            {
                "ConsultantLevel",
                "TopSellerStartedAt",
                "TopSellerLastEvaluatedPeriodStart"
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_ConsultantProfiles_ConsultantLevel_TopSellerStartedAt_TopSellerLastEvaluatedPeriodStart",
            table: "ConsultantProfiles");

        migrationBuilder.DropColumn(
            name: "TopSellerStartedAt",
            table: "ConsultantProfiles");

        migrationBuilder.DropColumn(
            name: "TopSellerLastEvaluatedPeriodStart",
            table: "ConsultantProfiles");

        migrationBuilder.DropColumn(
            name: "TopSellerLastEvaluatedAt",
            table: "ConsultantProfiles");

        migrationBuilder.DropColumn(
            name: "TopSellerRewardLevel",
            table: "ConsultantProfiles");
    }
}