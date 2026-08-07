using DentalDashboard.Infrastracture.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalDashboard.Infrastracture.Migrations;

[DbContext(typeof(DentalContext))]
[Migration("20260806120000_AddTestConsultantLifecycle")]
public sealed class AddTestConsultantLifecycle : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>("TestStartedAt", "ConsultantProfiles", "datetime2", nullable: true);
        migrationBuilder.AddColumn<DateTime>("TestCompletedAt", "ConsultantProfiles", "datetime2", nullable: true);
        migrationBuilder.AddColumn<bool>("TestPassed", "ConsultantProfiles", "bit", nullable: true);
        migrationBuilder.Sql(
            "UPDATE ConsultantProfiles SET TestStartedAt = GETUTCDATE() " +
            "WHERE ConsultantLevel = 1 AND TestStartedAt IS NULL");
        migrationBuilder.CreateIndex(
            name: "IX_ConsultantProfiles_ConsultantLevel_TestStartedAt_TestCompletedAt",
            table: "ConsultantProfiles",
            columns: new[] { "ConsultantLevel", "TestStartedAt", "TestCompletedAt" });
        migrationBuilder.CreateIndex(
            name: "IX_LeadAssignments_ConsultantProfileId_AssignedAt_PickUp",
            table: "LeadAssignments",
            columns: new[] { "ConsultantProfileId", "AssignedAt", "PickUp" });
        migrationBuilder.CreateIndex(
            name: "IX_LeadAssignments_IsDeleted_AssignmentType_LeadAssignmentState_ConsultantProfileId_PickUp",
            table: "LeadAssignments",
            columns: new[] { "IsDeleted", "AssignmentType", "LeadAssignmentState", "ConsultantProfileId", "PickUp" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex("IX_ConsultantProfiles_ConsultantLevel_TestStartedAt_TestCompletedAt", "ConsultantProfiles");
        migrationBuilder.DropIndex("IX_LeadAssignments_ConsultantProfileId_AssignedAt_PickUp", "LeadAssignments");
        migrationBuilder.DropIndex("IX_LeadAssignments_IsDeleted_AssignmentType_LeadAssignmentState_ConsultantProfileId_PickUp", "LeadAssignments");
        migrationBuilder.DropColumn("TestStartedAt", "ConsultantProfiles");
        migrationBuilder.DropColumn("TestCompletedAt", "ConsultantProfiles");
        migrationBuilder.DropColumn("TestPassed", "ConsultantProfiles");
    }
}
