using System;
using DentalDashboard.Infrastracture.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace DentalDashboard.Infrastracture.Migrations;
[DbContext(typeof(DentalContext))]
[Migration("20260822120000_AddSecretaryFollowUpContacted")]
public partial class AddSecretaryFollowUpContacted : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(name: "SecretaryFollowUpContacted", table: "Reservations", type: "bit", nullable: true);
        migrationBuilder.CreateIndex(name: "IX_Reservations_SecretaryAnnouncementUserId_SecretaryAnnouncementUpdatedAt", table: "Reservations", columns: new[] { "SecretaryAnnouncementUserId", "SecretaryAnnouncementUpdatedAt" });
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_Reservations_SecretaryAnnouncementUserId_SecretaryAnnouncementUpdatedAt", table: "Reservations");
        migrationBuilder.DropColumn(name: "SecretaryFollowUpContacted", table: "Reservations");
    }
}
