using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalDashboard.Infrastracture.Migrations;

public partial class AddSecretaryAnnouncementWorkflow : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>("SecretaryAnnouncement", "Reservations", "nvarchar(1000)", maxLength: 1000, nullable: true);
        migrationBuilder.AddColumn<int>("SecretaryAnnouncementStatus", "Reservations", "int", nullable: true);
        migrationBuilder.AddColumn<DateTime>("SecretaryAnnouncementUpdatedAt", "Reservations", "datetime2", nullable: true);
        migrationBuilder.AddColumn<Guid>("SecretaryAnnouncementUserId", "Reservations", "uniqueidentifier", nullable: true);

        migrationBuilder.CreateIndex(
            "IX_Reservations_SecretaryAnnouncementUserId",
            "Reservations",
            "SecretaryAnnouncementUserId");
        migrationBuilder.CreateIndex(
            "IX_Reservations_SecretaryAnnouncementStatus_ReservationAt_IsCanceled",
            "Reservations",
            new[] { "SecretaryAnnouncementStatus", "ReservationAt", "IsCanceled" });
        migrationBuilder.AddForeignKey(
            "FK_Reservations_Users_SecretaryAnnouncementUserId",
            "Reservations",
            "SecretaryAnnouncementUserId",
            "Users",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey("FK_Reservations_Users_SecretaryAnnouncementUserId", "Reservations");
        migrationBuilder.DropIndex("IX_Reservations_SecretaryAnnouncementUserId", "Reservations");
        migrationBuilder.DropIndex("IX_Reservations_SecretaryAnnouncementStatus_ReservationAt_IsCanceled", "Reservations");
        migrationBuilder.DropColumn("SecretaryAnnouncement", "Reservations");
        migrationBuilder.DropColumn("SecretaryAnnouncementStatus", "Reservations");
        migrationBuilder.DropColumn("SecretaryAnnouncementUpdatedAt", "Reservations");
        migrationBuilder.DropColumn("SecretaryAnnouncementUserId", "Reservations");
    }
}
