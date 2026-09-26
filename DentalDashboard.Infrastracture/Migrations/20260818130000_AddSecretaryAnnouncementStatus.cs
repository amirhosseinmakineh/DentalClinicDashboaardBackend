using DentalDashboard.Infrastracture.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalDashboard.Infrastracture.Migrations;

[DbContext(typeof(DentalContext))]
[Migration("20260818130000_AddSecretaryAnnouncementStatus")]
public partial class AddSecretaryAnnouncementStatus : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "SecretaryAnnouncementStatus",
            table: "Reservations",
            type: "int",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Reservations_SecretaryAnnouncementStatus",
            table: "Reservations",
            column: "SecretaryAnnouncementStatus");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Reservations_SecretaryAnnouncementStatus",
            table: "Reservations");

        migrationBuilder.DropColumn(
            name: "SecretaryAnnouncementStatus",
            table: "Reservations");
    }
}
