using DentalDashboard.Infrastracture.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalDashboard.Infrastracture.Migrations;

[DbContext(typeof(DentalContext))]
[Migration("20260807120000_AddSellerConsultantLifecycle")]
public sealed class AddSellerConsultantLifecycle : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>("SellerStartedAt", "ConsultantProfiles", "datetime2", nullable: true);
        migrationBuilder.AddColumn<DateTime>("SellerEvaluatedAt", "ConsultantProfiles", "datetime2", nullable: true);
        migrationBuilder.Sql(
            "UPDATE ConsultantProfiles SET SellerStartedAt = GETUTCDATE() " +
            "WHERE ConsultantLevel = 2 AND SellerStartedAt IS NULL");
        migrationBuilder.CreateIndex(
            name: "IX_ConsultantProfiles_ConsultantLevel_SellerStartedAt_SellerEvaluatedAt",
            table: "ConsultantProfiles",
            columns: new[] { "ConsultantLevel", "SellerStartedAt", "SellerEvaluatedAt" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            "IX_ConsultantProfiles_ConsultantLevel_SellerStartedAt_SellerEvaluatedAt",
            "ConsultantProfiles");
        migrationBuilder.DropColumn("SellerStartedAt", "ConsultantProfiles");
        migrationBuilder.DropColumn("SellerEvaluatedAt", "ConsultantProfiles");
    }
}
