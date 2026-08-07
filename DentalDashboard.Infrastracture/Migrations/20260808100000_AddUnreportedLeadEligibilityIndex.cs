using DentalDashboard.Infrastracture.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalDashboard.Infrastracture.Migrations;

[DbContext(typeof(DentalContext))]
[Migration("20260808100000_AddUnreportedLeadEligibilityIndex")]
public sealed class AddUnreportedLeadEligibilityIndex : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.CreateIndex(
            name: "IX_LeadAssignments_ConsultantProfileId_ReportSubmittedAt",
            table: "LeadAssignments",
            columns: new[] { "ConsultantProfileId", "ReportSubmittedAt" });

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropIndex(
            name: "IX_LeadAssignments_ConsultantProfileId_ReportSubmittedAt",
            table: "LeadAssignments");
}
