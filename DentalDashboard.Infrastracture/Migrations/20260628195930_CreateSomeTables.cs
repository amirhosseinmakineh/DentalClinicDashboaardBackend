using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalDashboard.Infrastracture.Migrations
{
    /// <inheritdoc />
    public partial class CreateSomeTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttendancePrediction",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "AttendanceProbabilityPercent",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "PatientCity",
                table: "Reservations");

            migrationBuilder.AddColumn<string>(
                name: "SecondaryPhoneNumber",
                table: "Reservations",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AttendanceProbabilityPercent",
                table: "LeadAssignments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BusinessName",
                table: "LeadAssignments",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PatientCity",
                table: "LeadAssignments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PatientRegion",
                table: "LeadAssignments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_LeadAssignments_ReportSubmittedAt",
                table: "LeadAssignments",
                column: "ReportSubmittedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LeadAssignments_ReportSubmittedAt",
                table: "LeadAssignments");

            migrationBuilder.DropColumn(
                name: "SecondaryPhoneNumber",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "AttendanceProbabilityPercent",
                table: "LeadAssignments");

            migrationBuilder.DropColumn(
                name: "BusinessName",
                table: "LeadAssignments");

            migrationBuilder.DropColumn(
                name: "PatientCity",
                table: "LeadAssignments");

            migrationBuilder.DropColumn(
                name: "PatientRegion",
                table: "LeadAssignments");

            migrationBuilder.AddColumn<string>(
                name: "AttendancePrediction",
                table: "Reservations",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "AttendanceProbabilityPercent",
                table: "Reservations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PatientCity",
                table: "Reservations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }
    }
}
