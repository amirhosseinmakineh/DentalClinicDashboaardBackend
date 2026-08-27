using DentalDashboard.Infrastracture.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalDashboard.Infrastracture.Migrations;

[DbContext(typeof(DentalContext))]
[Migration("20260825120000_StructureSecretaryFollowUps")]
public sealed class StructureSecretaryFollowUps : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(name: "DoctorName", table: "Reservations", type: "nvarchar(120)", maxLength: 120, nullable: true, oldClrType: typeof(string), oldType: "nvarchar(150)", oldMaxLength: 150, oldNullable: true);
        migrationBuilder.AlterColumn<string>(name: "SecretaryAnnouncement", table: "Reservations", type: "nvarchar(2000)", maxLength: 2000, nullable: true, oldClrType: typeof(string), oldType: "nvarchar(1000)", oldMaxLength: 1000, oldNullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "SecretaryFollowUpCreatedAt", table: "Reservations", type: "datetime2", nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "SecretaryFollowUpDeletedAt", table: "Reservations", type: "datetime2", nullable: true);
        migrationBuilder.Sql("UPDATE Reservations SET SecretaryFollowUpCreatedAt = SecretaryAnnouncementUpdatedAt WHERE SecretaryAnnouncementUpdatedAt IS NOT NULL AND SecretaryAnnouncementUserId IS NOT NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "SecretaryFollowUpCreatedAt", table: "Reservations");
        migrationBuilder.DropColumn(name: "SecretaryFollowUpDeletedAt", table: "Reservations");
        migrationBuilder.AlterColumn<string>(name: "DoctorName", table: "Reservations", type: "nvarchar(150)", maxLength: 150, nullable: true, oldClrType: typeof(string), oldType: "nvarchar(120)", oldMaxLength: 120, oldNullable: true);
        migrationBuilder.AlterColumn<string>(name: "SecretaryAnnouncement", table: "Reservations", type: "nvarchar(1000)", maxLength: 1000, nullable: true, oldClrType: typeof(string), oldType: "nvarchar(2000)", oldMaxLength: 2000, oldNullable: true);
    }
}
