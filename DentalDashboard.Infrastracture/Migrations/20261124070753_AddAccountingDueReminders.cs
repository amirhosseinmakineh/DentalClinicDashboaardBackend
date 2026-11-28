using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalDashboard.Infrastracture.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountingDueReminders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountingReminders",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    TargetId = table.Column<long>(type: "bigint", nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingReminders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountingReminders_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccountingReminderDeliveries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReminderId = table.Column<long>(type: "bigint", nullable: false),
                    DueDate = table.Column<DateTime>(type: "date", nullable: false),
                    ReminderDate = table.Column<DateTime>(type: "date", nullable: false),
                    LeaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LeaseUntilUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SentAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Attempts = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingReminderDeliveries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountingReminderDeliveries_AccountingReminders_ReminderId",
                        column: x => x.ReminderId,
                        principalTable: "AccountingReminders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountingReminderDeliveries_LeaseId",
                table: "AccountingReminderDeliveries",
                column: "LeaseId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountingReminderDeliveries_ReminderId_DueDate_ReminderDate",
                table: "AccountingReminderDeliveries",
                columns: new[] { "ReminderId", "DueDate", "ReminderDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountingReminders_Enabled_Id",
                table: "AccountingReminders",
                columns: new[] { "Enabled", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_AccountingReminders_UserId_Kind_TargetId",
                table: "AccountingReminders",
                columns: new[] { "UserId", "Kind", "TargetId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountingReminderDeliveries");

            migrationBuilder.DropTable(
                name: "AccountingReminders");
        }
    }
}
