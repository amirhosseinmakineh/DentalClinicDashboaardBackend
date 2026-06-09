using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalDashboard.Infrastracture.Migrations
{
    /// <inheritdoc />
    public partial class CreateSomePropertyToSomeTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AssignedAt",
                table: "LeadAssignments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AssignmentType",
                table: "LeadAssignments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "CallDeadlineAt",
                table: "LeadAssignments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "NotificationSent",
                table: "LeadAssignments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresThreeMinuteCall",
                table: "LeadAssignments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SmsSent",
                table: "LeadAssignments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsOnline",
                table: "ConsultantProfiles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastOfflineAt",
                table: "ConsultantProfiles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastOnlineAt",
                table: "ConsultantProfiles",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssignedAt",
                table: "LeadAssignments");

            migrationBuilder.DropColumn(
                name: "AssignmentType",
                table: "LeadAssignments");

            migrationBuilder.DropColumn(
                name: "CallDeadlineAt",
                table: "LeadAssignments");

            migrationBuilder.DropColumn(
                name: "NotificationSent",
                table: "LeadAssignments");

            migrationBuilder.DropColumn(
                name: "RequiresThreeMinuteCall",
                table: "LeadAssignments");

            migrationBuilder.DropColumn(
                name: "SmsSent",
                table: "LeadAssignments");

            migrationBuilder.DropColumn(
                name: "IsOnline",
                table: "ConsultantProfiles");

            migrationBuilder.DropColumn(
                name: "LastOfflineAt",
                table: "ConsultantProfiles");

            migrationBuilder.DropColumn(
                name: "LastOnlineAt",
                table: "ConsultantProfiles");
        }
    }
}
