using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalDashboard.Infrastracture.Migrations
{
    /// <inheritdoc />
    public partial class FixPushSubscriptionForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PushSubscriptions_Users_UserId1",
                table: "PushSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_PushSubscriptions_UserId1",
                table: "PushSubscriptions");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "PushSubscriptions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UserId1",
                table: "PushSubscriptions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_PushSubscriptions_UserId1",
                table: "PushSubscriptions",
                column: "UserId1");

            migrationBuilder.AddForeignKey(
                name: "FK_PushSubscriptions_Users_UserId1",
                table: "PushSubscriptions",
                column: "UserId1",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
