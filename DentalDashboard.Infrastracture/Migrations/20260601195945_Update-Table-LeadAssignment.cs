using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalDashboard.Infrastracture.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTableLeadAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LeadAssignments_ConsultantProfiles_ConsultantProfileId",
                table: "LeadAssignments");

            migrationBuilder.DropColumn(
                name: "AssignedAt",
                table: "LeadAssignments");

            migrationBuilder.DropColumn(
                name: "CalledAt",
                table: "LeadAssignments");

            migrationBuilder.DropColumn(
                name: "ExpireAt",
                table: "LeadAssignments");

            migrationBuilder.DropColumn(
                name: "IsCalled",
                table: "LeadAssignments");

            migrationBuilder.DropColumn(
                name: "IsExpired",
                table: "LeadAssignments");

            migrationBuilder.DropColumn(
                name: "IsPenaltyApplied",
                table: "LeadAssignments");

            migrationBuilder.RenameColumn(
                name: "CustomerPhoneNumber",
                table: "LeadAssignments",
                newName: "UserName");

            migrationBuilder.AlterColumn<long>(
                name: "ConsultantProfileId",
                table: "LeadAssignments",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<int>(
                name: "LeadAssignmentState",
                table: "LeadAssignments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "LeadAssignments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_LeadAssignments_ConsultantProfiles_ConsultantProfileId",
                table: "LeadAssignments",
                column: "ConsultantProfileId",
                principalTable: "ConsultantProfiles",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LeadAssignments_ConsultantProfiles_ConsultantProfileId",
                table: "LeadAssignments");

            migrationBuilder.DropColumn(
                name: "LeadAssignmentState",
                table: "LeadAssignments");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "LeadAssignments");

            migrationBuilder.RenameColumn(
                name: "UserName",
                table: "LeadAssignments",
                newName: "CustomerPhoneNumber");

            migrationBuilder.AlterColumn<long>(
                name: "ConsultantProfileId",
                table: "LeadAssignments",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AssignedAt",
                table: "LeadAssignments",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "CalledAt",
                table: "LeadAssignments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpireAt",
                table: "LeadAssignments",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsCalled",
                table: "LeadAssignments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsExpired",
                table: "LeadAssignments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPenaltyApplied",
                table: "LeadAssignments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddForeignKey(
                name: "FK_LeadAssignments_ConsultantProfiles_ConsultantProfileId",
                table: "LeadAssignments",
                column: "ConsultantProfileId",
                principalTable: "ConsultantProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
