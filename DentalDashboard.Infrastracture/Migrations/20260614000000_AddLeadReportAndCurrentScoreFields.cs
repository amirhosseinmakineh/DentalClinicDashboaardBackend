using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalDashboard.Infrastracture.Migrations
{
    /// <inheritdoc />
    public partial class AddLeadReportAndCurrentScoreFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrentScore",
                table: "ConsultantProfiles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CallResult",
                table: "LeadAssignments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ContactedAt",
                table: "LeadAssignments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReportDescription",
                table: "LeadAssignments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReportSubmittedAt",
                table: "LeadAssignments",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentScore",
                table: "ConsultantProfiles");

            migrationBuilder.DropColumn(
                name: "CallResult",
                table: "LeadAssignments");

            migrationBuilder.DropColumn(
                name: "ContactedAt",
                table: "LeadAssignments");

            migrationBuilder.DropColumn(
                name: "ReportDescription",
                table: "LeadAssignments");

            migrationBuilder.DropColumn(
                name: "ReportSubmittedAt",
                table: "LeadAssignments");
        }
    }
}
