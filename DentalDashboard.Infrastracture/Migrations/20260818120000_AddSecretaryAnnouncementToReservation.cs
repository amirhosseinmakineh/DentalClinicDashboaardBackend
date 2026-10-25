using DentalDashboard.Infrastracture.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalDashboard.Infrastracture.Migrations
{
    [DbContext(typeof(DentalContext))]
    [Migration("20260818120000_AddSecretaryAnnouncementToReservation")]
    public partial class AddSecretaryAnnouncementToReservation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SecretaryAnnouncement",
                table: "Reservations",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SecretaryAnnouncementUpdatedAt",
                table: "Reservations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SecretaryAnnouncementUserId",
                table: "Reservations",
                type: "uniqueidentifier",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SecretaryAnnouncement",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "SecretaryAnnouncementUpdatedAt",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "SecretaryAnnouncementUserId",
                table: "Reservations");
        }
    }
}
