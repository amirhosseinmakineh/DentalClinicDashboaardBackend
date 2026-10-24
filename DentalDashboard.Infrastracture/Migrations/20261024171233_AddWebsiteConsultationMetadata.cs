using Microsoft.EntityFrameworkCore.Migrations;

namespace DentalDashboard.Infrastracture.Migrations;

public partial class AddWebsiteConsultationMetadata : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<long>("ConsultationServiceId", "LeadAssignments", type: "bigint", nullable: true);
        migrationBuilder.AddColumn<string>("ConsultationNotes", "LeadAssignments", type: "nvarchar(1000)", maxLength: 1000, nullable: true);
        migrationBuilder.AddColumn<Guid>("ConsultationRequestId", "LeadAssignments", type: "uniqueidentifier", nullable: true);
        migrationBuilder.CreateIndex("IX_LeadAssignments_ConsultationServiceId", "LeadAssignments", "ConsultationServiceId");
        migrationBuilder.CreateIndex("IX_LeadAssignments_ConsultationRequestId", "LeadAssignments", "ConsultationRequestId", unique: true, filter: "[ConsultationRequestId] IS NOT NULL");
        migrationBuilder.AddForeignKey("FK_LeadAssignments_SecretarySaleServices_ConsultationServiceId", "LeadAssignments", "ConsultationServiceId", "SecretarySaleServices", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        // SourceType already exists. Preserve every historical value without inferring origin.
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey("FK_LeadAssignments_SecretarySaleServices_ConsultationServiceId", "LeadAssignments");
        migrationBuilder.DropIndex("IX_LeadAssignments_ConsultationServiceId", "LeadAssignments");
        migrationBuilder.DropIndex("IX_LeadAssignments_ConsultationRequestId", "LeadAssignments");
        migrationBuilder.DropColumn("ConsultationServiceId", "LeadAssignments");
        migrationBuilder.DropColumn("ConsultationNotes", "LeadAssignments");
        migrationBuilder.DropColumn("ConsultationRequestId", "LeadAssignments");
    }
}
