using DentalDashboard.Infrastracture.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalDashboard.Infrastracture.Migrations;

[DbContext(typeof(DentalContext))]
[Migration("20260908160000_AddConsultantPreferredLeadSource")]
public partial class AddConsultantPreferredLeadSource : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "PreferredLeadSourceType",
            table: "ConsultantProfiles",
            type: "int",
            nullable: true);

        migrationBuilder.AddCheckConstraint(
            name: "CK_ConsultantProfiles_PreferredLeadSourceType",
            table: "ConsultantProfiles",
            sql: "[PreferredLeadSourceType] IS NULL OR [PreferredLeadSourceType] IN (1, 2)");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "CK_ConsultantProfiles_PreferredLeadSourceType",
            table: "ConsultantProfiles");

        migrationBuilder.DropColumn(
            name: "PreferredLeadSourceType",
            table: "ConsultantProfiles");
    }
}
