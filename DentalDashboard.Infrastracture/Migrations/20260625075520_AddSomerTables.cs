using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalDashboard.Infrastracture.Migrations
{
    /// <inheritdoc />
    public partial class AddSomerTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PatientUserId",
                table: "Reservations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_PatientUserId",
                table: "Reservations",
                column: "PatientUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_Users_PatientUserId",
                table: "Reservations",
                column: "PatientUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_Users_PatientUserId",
                table: "Reservations");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_PatientUserId",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "PatientUserId",
                table: "Reservations");
        }
    }
}
