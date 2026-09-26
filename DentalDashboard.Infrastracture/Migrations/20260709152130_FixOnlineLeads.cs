using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalDashboard.Infrastracture.Migrations
{
    /// <inheritdoc />
    public partial class FixOnlineLeads : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DispatchLevel",
                table: "LeadAssignments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastDispatchAt",
                table: "LeadAssignments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PickUp",
                table: "LeadAssignments",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DispatchLevel",
                table: "LeadAssignments");

            migrationBuilder.DropColumn(
                name: "LastDispatchAt",
                table: "LeadAssignments");

            migrationBuilder.DropColumn(
                name: "PickUp",
                table: "LeadAssignments");
        }
    }
}
