using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalDashboard.Infrastracture.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTableScoreLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ScoreType",
                table: "ScoreLogs",
                newName: "Source");

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "ScoreLogs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "LeadAssignmentId",
                table: "ScoreLogs",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Reason",
                table: "ScoreLogs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "ScoreLogs",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<TimeOnly>(
                name: "CheckOutTime",
                table: "Attendances",
                type: "time",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0),
                oldClrType: typeof(TimeOnly),
                oldType: "time",
                oldNullable: true);

            migrationBuilder.AlterColumn<TimeOnly>(
                name: "CheckInTime",
                table: "Attendances",
                type: "time",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0),
                oldClrType: typeof(TimeOnly),
                oldType: "time",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScoreLogs_LeadAssignmentId",
                table: "ScoreLogs",
                column: "LeadAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ScoreLogs_UserId",
                table: "ScoreLogs",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ScoreLogs_LeadAssignments_LeadAssignmentId",
                table: "ScoreLogs",
                column: "LeadAssignmentId",
                principalTable: "LeadAssignments",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ScoreLogs_Users_UserId",
                table: "ScoreLogs",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScoreLogs_LeadAssignments_LeadAssignmentId",
                table: "ScoreLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_ScoreLogs_Users_UserId",
                table: "ScoreLogs");

            migrationBuilder.DropIndex(
                name: "IX_ScoreLogs_LeadAssignmentId",
                table: "ScoreLogs");

            migrationBuilder.DropIndex(
                name: "IX_ScoreLogs_UserId",
                table: "ScoreLogs");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "ScoreLogs");

            migrationBuilder.DropColumn(
                name: "LeadAssignmentId",
                table: "ScoreLogs");

            migrationBuilder.DropColumn(
                name: "Reason",
                table: "ScoreLogs");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "ScoreLogs");

            migrationBuilder.RenameColumn(
                name: "Source",
                table: "ScoreLogs",
                newName: "ScoreType");

            migrationBuilder.AlterColumn<TimeOnly>(
                name: "CheckOutTime",
                table: "Attendances",
                type: "time",
                nullable: true,
                oldClrType: typeof(TimeOnly),
                oldType: "time");

            migrationBuilder.AlterColumn<TimeOnly>(
                name: "CheckInTime",
                table: "Attendances",
                type: "time",
                nullable: true,
                oldClrType: typeof(TimeOnly),
                oldType: "time");
        }
    }
}
