using System;
using DentalDashboard.Infrastracture.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalDashboard.Infrastracture.Migrations;

[DbContext(typeof(DentalContext))]
[Migration("20260911090000_AddConsultantLeadClosure")]
public partial class AddConsultantLeadClosure : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "ClosedByConsultantAt",
            table: "LeadAssignments",
            type: "datetime2",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ClosureDescription",
            table: "LeadAssignments",
            type: "nvarchar(500)",
            maxLength: 500,
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "ClosureReason",
            table: "LeadAssignments",
            type: "int",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "ClosedByConsultantAt", table: "LeadAssignments");
        migrationBuilder.DropColumn(name: "ClosureDescription", table: "LeadAssignments");
        migrationBuilder.DropColumn(name: "ClosureReason", table: "LeadAssignments");
    }
}
