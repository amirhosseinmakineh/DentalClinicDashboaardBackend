using DentalDashboard.Infrastracture.Context;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DentalDashboard.Infrastracture.Migrations;

[DbContext(typeof(DentalContext))]
[Migration("20261026150000_AddConsultantActiveLeadListIndex")]
public partial class AddConsultantActiveLeadListIndex : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_LeadAssignments_Consultant_Active_AssignedAt_Id",
            table: "LeadAssignments",
            columns: new[] { "ConsultantProfileId", "AssignedAt", "Id" },
            descending: new[] { false, true, true },
            filter: "[IsDeleted] = 0 AND [ReportSubmittedAt] IS NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_LeadAssignments_Consultant_Active_AssignedAt_Id",
            table: "LeadAssignments");
    }
}
