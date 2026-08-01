using DentalDashboard.Infrastracture.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalDashboard.Infrastracture.Migrations;

[DbContext(typeof(DentalContext))]
[Migration("20260801120000_AddConsultantLevel")]
public sealed class AddConsultantLevel : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<byte>(
            name: "ConsultantLevel",
            table: "ConsultantProfiles",
            type: "tinyint",
            nullable: false,
            defaultValue: (byte)2);

        migrationBuilder.AddCheckConstraint(
            name: "CK_ConsultantProfiles_ConsultantLevel",
            table: "ConsultantProfiles",
            sql: "[ConsultantLevel] IN (1, 2, 3)");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "CK_ConsultantProfiles_ConsultantLevel",
            table: "ConsultantProfiles");

        migrationBuilder.DropColumn(
            name: "ConsultantLevel",
            table: "ConsultantProfiles");
    }
}
