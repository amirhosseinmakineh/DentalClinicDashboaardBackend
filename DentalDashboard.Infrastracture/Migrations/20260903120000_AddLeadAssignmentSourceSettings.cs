using System;
using DentalDashboard.Infrastracture.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalDashboard.Infrastracture.Migrations;

[DbContext(typeof(DentalContext))]
[Migration("20260903120000_AddLeadAssignmentSourceSettings")]
public partial class AddLeadAssignmentSourceSettings : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "LeadAssignmentSettings",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false),
                AssignmentSourceType = table.Column<int>(type: "int", nullable: false),
                UpdatedByAdminId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LeadAssignmentSettings", x => x.Id);
                table.CheckConstraint("CK_LeadAssignmentSettings_Singleton", "[Id] = 1");
                table.CheckConstraint("CK_LeadAssignmentSettings_SourceType", "[AssignmentSourceType] IN (1, 2)");
                table.ForeignKey(
                    name: "FK_LeadAssignmentSettings_Users_UpdatedByAdminId",
                    column: x => x.UpdatedByAdminId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.InsertData(
            table: "LeadAssignmentSettings",
            columns: new[] { "Id", "AssignmentSourceType", "CreatedAt", "UpdatedByAdminId", "UpdatedAt", "IsDeleted", "DeletedAt" },
            columnTypes: new[] { "bigint", "int", "datetime2", "uniqueidentifier", "datetime2", "bit", "datetime2" },
            values: new object[] { 1L, 1, new DateTime(2026, 9, 3, 0, 0, 0, DateTimeKind.Utc), null, null, false, null });

        migrationBuilder.CreateIndex("IX_LeadAssignmentSettings_UpdatedByAdminId", "LeadAssignmentSettings", "UpdatedByAdminId");
        migrationBuilder.CreateIndex(
            "IX_LeadAssignments_IsDeleted_LeadAssignmentState_ConsultantProfileId_CreatedAt",
            "LeadAssignments",
            new[] { "IsDeleted", "LeadAssignmentState", "ConsultantProfileId", "CreatedAt" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("LeadAssignmentSettings");
        migrationBuilder.DropIndex(
            name: "IX_LeadAssignments_IsDeleted_LeadAssignmentState_ConsultantProfileId_CreatedAt",
            table: "LeadAssignments");
    }
}
