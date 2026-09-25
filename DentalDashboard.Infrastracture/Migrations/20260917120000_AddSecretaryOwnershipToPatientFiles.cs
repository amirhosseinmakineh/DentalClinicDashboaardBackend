using DentalDashboard.Infrastracture.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalDashboard.Infrastracture.Migrations;

[DbContext(typeof(DentalContext))]
[Migration("20260917120000_AddSecretaryOwnershipToPatientFiles")]
public partial class AddSecretaryOwnershipToPatientFiles : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "SecretaryUserId",
            table: "PatientFiles",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_PatientFiles_SecretaryUserId",
            table: "PatientFiles",
            column: "SecretaryUserId");

        migrationBuilder.CreateIndex(
            name: "IX_PatientFiles_SecretaryUserId_PhoneNumber",
            table: "PatientFiles",
            columns: new[] { "SecretaryUserId", "PhoneNumber" },
            unique: true,
            filter: "[SecretaryUserId] IS NOT NULL AND [IsDeleted] = 0");

        migrationBuilder.AddForeignKey(
            name: "FK_PatientFiles_Users_SecretaryUserId",
            table: "PatientFiles",
            column: "SecretaryUserId",
            principalTable: "Users",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_PatientFiles_Users_SecretaryUserId",
            table: "PatientFiles");

        migrationBuilder.DropIndex(
            name: "IX_PatientFiles_SecretaryUserId_PhoneNumber",
            table: "PatientFiles");

        migrationBuilder.DropIndex(
            name: "IX_PatientFiles_SecretaryUserId",
            table: "PatientFiles");

        migrationBuilder.DropColumn(
            name: "SecretaryUserId",
            table: "PatientFiles");
    }
}
