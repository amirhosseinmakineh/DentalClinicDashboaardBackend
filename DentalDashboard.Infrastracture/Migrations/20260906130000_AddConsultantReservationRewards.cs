using DentalDashboard.Infrastracture.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalDashboard.Infrastracture.Migrations;

[DbContext(typeof(DentalContext))]
[Migration("20260906130000_AddConsultantReservationRewards")]
public partial class AddConsultantReservationRewards : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(name: "ConsultantRewardApprovedByAdmin", table: "Reservations", type: "bit", nullable: true);
        migrationBuilder.AddColumn<decimal>(name: "ConsultantRewardAmount", table: "Reservations", type: "decimal(18,2)", nullable: true);
        migrationBuilder.AddColumn<Guid>(name: "ConsultantRewardReviewedByAdminId", table: "Reservations", type: "uniqueidentifier", nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "ConsultantRewardReviewedAt", table: "Reservations", type: "datetime2", nullable: true);
        migrationBuilder.CreateIndex(
            name: "IX_Reservations_ConsultantRewardApprovedByAdmin_SecretaryReviewedAt",
            table: "Reservations",
            columns: new[] { "ConsultantRewardApprovedByAdmin", "SecretaryReviewedAt" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_Reservations_ConsultantRewardApprovedByAdmin_SecretaryReviewedAt", table: "Reservations");
        migrationBuilder.DropColumn(name: "ConsultantRewardApprovedByAdmin", table: "Reservations");
        migrationBuilder.DropColumn(name: "ConsultantRewardAmount", table: "Reservations");
        migrationBuilder.DropColumn(name: "ConsultantRewardReviewedByAdminId", table: "Reservations");
        migrationBuilder.DropColumn(name: "ConsultantRewardReviewedAt", table: "Reservations");
    }
}
