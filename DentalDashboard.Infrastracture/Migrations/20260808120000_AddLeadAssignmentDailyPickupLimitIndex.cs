using DentalDashboard.Infrastracture.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalDashboard.Infrastracture.Migrations;

[DbContext(typeof(DentalContext))]
[Migration("20260808120000_AddLeadAssignmentDailyPickupLimitIndex")]
public sealed class AddLeadAssignmentDailyPickupLimitIndex : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_LeadAssignments_ConsultantProfileId_AssignedAt_PickUp",
            table: "LeadAssignments");

        migrationBuilder.CreateIndex(
            name: "IX_LeadAssignments_ConsultantProfileId_PickUp_IsDeleted_AssignedAt",
            table: "LeadAssignments",
            columns: new[] { "ConsultantProfileId", "PickUp", "IsDeleted", "AssignedAt" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_LeadAssignments_ConsultantProfileId_PickUp_IsDeleted_AssignedAt",
            table: "LeadAssignments");

        migrationBuilder.CreateIndex(
            name: "IX_LeadAssignments_ConsultantProfileId_AssignedAt_PickUp",
            table: "LeadAssignments",
            columns: new[] { "ConsultantProfileId", "AssignedAt", "PickUp" });
    }
}
