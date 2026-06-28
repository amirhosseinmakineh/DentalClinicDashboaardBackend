using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalDashboard.Infrastracture.Migrations
{
    /// <inheritdoc />
    public partial class CreateSomeTable2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AttendanceConfirmationStatus",
                table: "Reservations",
                type: "int",
                nullable: false,
                defaultValue: 0);

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

            migrationBuilder.AddColumn<DateTime>(
                name: "AttendanceScoreAppliedAt",
                table: "Reservations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AttendanceScoreValue",
                table: "Reservations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ConsultantAttendanceConfirmedAt",
                table: "Reservations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConsultantAttendanceNote",
                table: "Reservations",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ConsultantSaysPatientAttended",
                table: "Reservations",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAttendanceScoreApplied",
                table: "Reservations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PatientCity",
                table: "Reservations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "SecretaryApprovedConsultantConfirmation",
                table: "Reservations",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecretaryReviewNote",
                table: "Reservations",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SecretaryReviewedAt",
                table: "Reservations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SecretaryUserId",
                table: "Reservations",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttendanceConfirmationStatus",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "AttendancePrediction",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "AttendanceProbabilityPercent",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "AttendanceScoreAppliedAt",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "AttendanceScoreValue",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "ConsultantAttendanceConfirmedAt",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "ConsultantAttendanceNote",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "ConsultantSaysPatientAttended",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "IsAttendanceScoreApplied",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "PatientCity",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "SecretaryApprovedConsultantConfirmation",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "SecretaryReviewNote",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "SecretaryReviewedAt",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "SecretaryUserId",
                table: "Reservations");
        }
    }
}
