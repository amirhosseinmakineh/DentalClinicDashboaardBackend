using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace DentalDashboard.Infrastracture.Migrations;

public partial class WebsiteConsultationRequests : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Upgrade the existing codex-lflotm schema. Never infer historical origins
        // or bootstrap unrelated tables from the stale pre-existing snapshot.
        migrationBuilder.Sql("""
            IF COL_LENGTH('LeadAssignments', 'SourceType') IS NULL
                THROW 51000, 'Existing lead-source migration must be applied before Website consultation upgrade.', 1;
            IF OBJECT_ID('SecretarySaleServices', 'U') IS NULL
                THROW 51000, 'Existing clinic service catalog must be installed before Website consultation upgrade.', 1;
            """);
        migrationBuilder.AddColumn<string>(name: "ConsultationDescription", table: "LeadAssignments",
            type: "nvarchar(1000)", maxLength: 1000, nullable: true);
        migrationBuilder.AddColumn<string>(name: "ConsultationRequestHash", table: "LeadAssignments",
            type: "nvarchar(64)", maxLength: 64, nullable: true);
        migrationBuilder.AddColumn<Guid>(name: "ConsultationRequestId", table: "LeadAssignments",
            type: "uniqueidentifier", nullable: true);
        migrationBuilder.CreateTable(
            name: "LeadRequestedService",
            columns: table => new
            {
                LeadAssignmentId = table.Column<long>(type: "bigint", nullable: false),
                ServiceId = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LeadRequestedService", x => new { x.LeadAssignmentId, x.ServiceId });
                table.ForeignKey(name: "FK_LeadRequestedService_LeadAssignments_LeadAssignmentId",
                    column: x => x.LeadAssignmentId, principalTable: "LeadAssignments", principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(name: "FK_LeadRequestedService_SecretarySaleServices_ServiceId",
                    column: x => x.ServiceId, principalTable: "SecretarySaleServices", principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });
        migrationBuilder.CreateIndex(name: "IX_LeadRequestedService_ServiceId", table: "LeadRequestedService", column: "ServiceId");
        migrationBuilder.CreateIndex(name: "IX_LeadAssignments_ConsultationRequestId", table: "LeadAssignments",
            column: "ConsultationRequestId", unique: true, filter: "[ConsultationRequestId] IS NOT NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "LeadRequestedService");
        migrationBuilder.DropIndex(name: "IX_LeadAssignments_ConsultationRequestId", table: "LeadAssignments");
        migrationBuilder.DropColumn(name: "ConsultationDescription", table: "LeadAssignments");
        migrationBuilder.DropColumn(name: "ConsultationRequestHash", table: "LeadAssignments");
        migrationBuilder.DropColumn(name: "ConsultationRequestId", table: "LeadAssignments");
    }
}
